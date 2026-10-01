using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Sendly.Exceptions;
using Sendly.Resources;

namespace Sendly;

/// <summary>
/// Sendly API Client for sending SMS messages.
/// </summary>
public class SendlyClient : IDisposable
{
    /// <summary>
    /// SDK version.
    /// </summary>
    public const string Version = "4.3.0";

    /// <summary>
    /// Default API base URL.
    /// </summary>
    public const string DefaultBaseUrl = "https://sendly.live/api/v1";

    private static readonly TimeSpan MaxRetryWait = TimeSpan.FromSeconds(60);

    private static bool WaitsOut(RateLimitException e) =>
        (e.ApiErrorCode is null or "rate_limit_exceeded" or "provision_rate_limit" or "too_many_concurrent_verifications") &&
        (e.RetryAfter ?? TimeSpan.Zero) <= MaxRetryWait;

    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly int _maxRetries;
    private bool _disposed;

    /// <summary>
    /// Gets the Messages resource.
    /// </summary>
    public MessagesResource Messages { get; }

    /// <summary>
    /// Gets the Webhooks resource.
    /// </summary>
    public WebhooksResource Webhooks { get; }

    /// <summary>
    /// Gets the Account resource.
    /// </summary>
    public AccountResource Account { get; }

    /// <summary>
    /// Gets the Verify resource.
    /// </summary>
    public VerifyResource Verify { get; }

    /// <summary>
    /// Gets the Templates resource (message templates, /templates).
    /// </summary>
    public TemplatesResource Templates { get; }

    /// <summary>
    /// Gets the MessageTemplates resource — reusable SMS message templates (/templates).
    /// </summary>
    public MessageTemplatesResource MessageTemplates { get; }

    /// <summary>
    /// Gets the Campaigns resource.
    /// </summary>
    public CampaignsResource Campaigns { get; }

    /// <summary>
    /// Gets the Contacts resource.
    /// </summary>
    public ContactsResource Contacts { get; }

    /// <summary>
    /// Gets the Media resource.
    /// </summary>
    public MediaResource Media { get; }

    /// <summary>
    /// Gets the Enterprise resource.
    /// </summary>
    public EnterpriseResource Enterprise { get; }

    /// <summary>
    /// Gets the Conversations resource.
    /// </summary>
    public ConversationsResource Conversations { get; }

    /// <summary>
    /// Gets the Labels resource.
    /// </summary>
    public LabelsResource Labels { get; }

    /// <summary>
    /// Gets the Drafts resource.
    /// </summary>
    public DraftsResource Drafts { get; }

    /// <summary>
    /// Gets the Rules resource.
    /// </summary>
    public RulesResource Rules { get; }

    /// <summary>
    /// Gets the BusinessUpgrade resource — entity-upgrade ("fork-with-new-number") flow.
    /// </summary>
    public BusinessUpgradeResource BusinessUpgrade { get; }

    /// <summary>
    /// Gets the Numbers resource — buy and manage phone numbers.
    /// </summary>
    public NumbersResource Numbers { get; }

    /// <summary>
    /// Gets the 10DLC resource — register for carrier review and text from local US numbers.
    /// </summary>
    public TenDlcResource TenDlc { get; }

    /// <summary>
    /// Gets the Links resource — branded URL shortening (gated behind the
    /// founder-only url_shortener flag; not yet publicly stable).
    /// </summary>
    public LinksResource Links { get; }

    /// <summary>
    /// Gets the WhatsApp resource — connect senders, manage Meta-reviewed
    /// templates, and check 24-hour windows.
    /// </summary>
    public WhatsAppResource WhatsApp { get; }

    /// <summary>
    /// Gets the RCS resource — register your brand and agent, list your
    /// agents, and pre-flight whether a recipient can receive RCS.
    /// </summary>
    public RcsResource Rcs { get; }

    /// <summary>
    /// Gets the Calls resource: place phone calls handled by your AI agents,
    /// list and inspect calls, end a call, and download recordings.
    /// </summary>
    public CallsResource Calls { get; }

    /// <summary>
    /// Gets the Voice resource: switch voice on for your numbers and choose
    /// how they answer, register emergency addresses, and manage the AI
    /// agents that talk on calls and the voices they speak with.
    /// </summary>
    /// <example>
    /// <code>
    /// var agent = await client.Voice.Agents.CreateAsync(new CreateVoiceAgentRequest { Name = "Front desk" });
    /// await client.Voice.Numbers.UpdateAsync("+15555550188", new UpdateVoiceNumberRequest
    /// {
    ///     VoiceEnabled = true,
    ///     VoiceMode = VoiceMode.Agent,
    ///     AgentId = agent.Id,
    /// });
    /// </code>
    /// </example>
    public VoiceResource Voice { get; }

    /// <summary>
    /// Creates a new Sendly client.
    /// </summary>
    /// <param name="apiKey">Your Sendly API key</param>
    /// <param name="options">Optional client configuration</param>
    public SendlyClient(string apiKey, SendlyClientOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new AuthenticationException("API key is required");

        _apiKey = apiKey;
        options ??= new SendlyClientOptions();
        options.OrganizationId ??= Environment.GetEnvironmentVariable("SENDLY_ORG_ID");
        _maxRetries = options.MaxRetries;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(NormalizeBaseUrl(options.BaseUrl ?? DefaultBaseUrl)),
            Timeout = options.Timeout
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"sendly-dotnet/{Version}");
        if (!string.IsNullOrEmpty(options.OrganizationId))
            _httpClient.DefaultRequestHeaders.Add("X-Organization-Id", options.OrganizationId);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { SendAssignedNulls } }
        };

        Messages = new MessagesResource(this);
        Webhooks = new WebhooksResource(this);
        Account = new AccountResource(this);
        Verify = new VerifyResource(this);
        Templates = new TemplatesResource(this);
        MessageTemplates = new MessageTemplatesResource(this);
        Campaigns = new CampaignsResource(this);
        Contacts = new ContactsResource(this);
        Media = new MediaResource(this);
        Enterprise = new EnterpriseResource(this);
        Conversations = new ConversationsResource(this);
        Labels = new LabelsResource(this);
        Drafts = new DraftsResource(this);
        Rules = new RulesResource(this);
        BusinessUpgrade = new BusinessUpgradeResource(this);
        Numbers = new NumbersResource(this);
        TenDlc = new TenDlcResource(this);
        Links = new LinksResource(this);
        WhatsApp = new WhatsAppResource(this);
        Rcs = new RcsResource(this);
        Calls = new CallsResource(this);
        Voice = new VoiceResource(this);
    }

    /// <summary>
    /// Makes a GET request.
    /// </summary>
    internal async Task<JsonDocument> GetAsync(string path, Dictionary<string, string>? queryParams = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(path, queryParams);
        return await ExecuteWithRetryAsync(() => _httpClient.GetAsync(url, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Makes a POST request.
    /// </summary>
    internal async Task<JsonDocument> PostAsync<T>(string path, T body, CancellationToken cancellationToken = default)
    {
        return await PostAsync(path, body, null, true, cancellationToken);
    }

    /// <summary>
    /// Makes a POST request with idempotency-key control.
    ///
    /// Every POST carries an Idempotency-Key header so the server can dedupe
    /// the SDK's own retries: an auto-generated key ("sendly-dotnet-retry-" +
    /// UUID) is created once per logical request and reused on every retry,
    /// after a timeout, a network error or a 5xx alike. A caller-supplied
    /// <paramref name="idempotencyKey"/> is sent verbatim and never changed.
    /// Pass <paramref name="autoIdempotencyKey"/> false to skip auto-generation
    /// (the batch endpoint dedupes header-less retries by content hash).
    /// </summary>
    internal async Task<JsonDocument> PostAsync<T>(string path, T body, string? idempotencyKey, bool autoIdempotencyKey, CancellationToken cancellationToken = default, bool retryUnknownOutcomes = true)
    {
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        var normalizedPath = NormalizePath(path);

        var explicitKey = NormalizeIdempotencyKey(idempotencyKey);
        var initialKey = explicitKey ?? (autoIdempotencyKey ? GenerateIdempotencyKey() : null);

        return await ExecuteWithRetryAsync(
            key =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, normalizedPath)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                if (key != null)
                    request.Headers.Add("Idempotency-Key", key);
                return _httpClient.SendAsync(request, cancellationToken);
            },
            initialKey,
            cancellationToken,
            retryUnknownOutcomes);
    }

    /// <summary>
    /// Makes a PATCH request.
    /// </summary>
    internal async Task<JsonDocument> PatchAsync<T>(string path, T body, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var normalizedPath = NormalizePath(path);

        return await ExecuteWithRetryAsync(
            () => _httpClient.PatchAsync(normalizedPath, content, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Makes a PATCH request carrying a caller-supplied Idempotency-Key. No
    /// key is generated when <paramref name="idempotencyKey"/> is absent; a
    /// supplied key is validated, sent verbatim, and never rotated.
    /// </summary>
    internal async Task<JsonDocument> PatchAsync<T>(string path, T body, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await SendJsonWithKeyAsync(HttpMethod.Patch, path, body, idempotencyKey, cancellationToken);
    }

    /// <summary>
    /// Makes a PUT request.
    /// </summary>
    internal async Task<JsonDocument> PutAsync<T>(string path, T body, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var normalizedPath = NormalizePath(path);

        return await ExecuteWithRetryAsync(
            () => _httpClient.PutAsync(normalizedPath, content, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Makes a PUT request carrying a caller-supplied Idempotency-Key, on the
    /// same terms as <see cref="PatchAsync{T}(string, T, string?, CancellationToken)"/>.
    /// </summary>
    internal async Task<JsonDocument> PutAsync<T>(string path, T body, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await SendJsonWithKeyAsync(HttpMethod.Put, path, body, idempotencyKey, cancellationToken);
    }

    private async Task<JsonDocument> SendJsonWithKeyAsync<T>(HttpMethod method, string path, T body, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        var normalizedPath = NormalizePath(path);
        var explicitKey = NormalizeIdempotencyKey(idempotencyKey);

        return await ExecuteWithRetryAsync(
            key =>
            {
                var request = new HttpRequestMessage(method, normalizedPath)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                if (key != null)
                    request.Headers.Add("Idempotency-Key", key);
                return _httpClient.SendAsync(request, cancellationToken);
            },
            explicitKey,
            cancellationToken);
    }

    /// <summary>
    /// Makes a POST request with raw HttpContent (multipart uploads). Carries
    /// an auto-generated Idempotency-Key, reused on every retry as for JSON
    /// POSTs.
    /// </summary>
    internal async Task<JsonDocument> PostContentAsync(string path, HttpContent content, CancellationToken cancellationToken = default, bool retryUnknownOutcomes = true)
    {
        var normalizedPath = NormalizePath(path);
        var initialKey = GenerateIdempotencyKey();

        return await ExecuteWithRetryAsync(
            key =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, normalizedPath)
                {
                    Content = content
                };
                if (key != null)
                    request.Headers.Add("Idempotency-Key", key);
                return _httpClient.SendAsync(request, cancellationToken);
            },
            initialKey,
            cancellationToken,
            retryUnknownOutcomes);
    }

    /// <summary>
    /// Makes a DELETE request.
    /// </summary>
    internal async Task<JsonDocument> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);
        return await ExecuteWithRetryAsync(
            () => _httpClient.DeleteAsync(normalizedPath, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Makes a DELETE request carrying a caller-supplied Idempotency-Key, on
    /// the same terms as <see cref="PatchAsync{T}(string, T, string?, CancellationToken)"/>.
    /// </summary>
    internal async Task<JsonDocument> DeleteAsync(string path, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);
        var explicitKey = NormalizeIdempotencyKey(idempotencyKey);

        return await ExecuteWithRetryAsync(
            key =>
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, normalizedPath);
                if (key != null)
                    request.Headers.Add("Idempotency-Key", key);
                return _httpClient.SendAsync(request, cancellationToken);
            },
            explicitKey,
            cancellationToken);
    }

    private static void SendAssignedNulls(JsonTypeInfo typeInfo)
    {
        if (!typeof(IAssignedProperties).IsAssignableFrom(typeInfo.Type))
            return;

        foreach (var property in typeInfo.Properties)
        {
            if (property.AttributeProvider is not MemberInfo member)
                continue;

            var name = member.Name;
            property.ShouldSerialize = (target, value) =>
                value != null || ((IAssignedProperties)target).IsAssigned(name);
        }
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.TrimStart('/');
        var queryStart = normalized.IndexOf('?');
        var pathPart = queryStart >= 0 ? normalized[..queryStart] : normalized;

        foreach (var segment in pathPart.Split('/'))
        {
            if (segment.Length == 0 || segment == "." || segment == "..")
                throw new ValidationException("An ID in the request path cannot be empty, '.' or '..'");
        }

        return normalized;
    }

    /// <summary>
    /// Ensures the base URL ends in a slash before it becomes
    /// <see cref="HttpClient.BaseAddress"/>. Request paths are relative
    /// ("messages"), and RFC 3986 reference resolution drops the last segment
    /// of a base that does not end in "/" — so a base of ".../api/v1" would
    /// resolve to ".../api/messages" and miss the versioned API entirely.
    /// </summary>
    private static string NormalizeBaseUrl(string baseUrl)
    {
        var trimmed = baseUrl.Trim();
        return trimmed.EndsWith('/') ? trimmed : trimmed + "/";
    }

    private string BuildUrl(string path, Dictionary<string, string>? queryParams)
    {
        var normalizedPath = NormalizePath(path);

        if (queryParams == null || queryParams.Count == 0)
            return normalizedPath;

        var query = string.Join("&", queryParams
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return string.IsNullOrEmpty(query) ? normalizedPath : $"{normalizedPath}?{query}";
    }

    private async Task<JsonDocument> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> requestFunc,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithRetryAsync(_ => requestFunc(), null, cancellationToken);
    }

    /// <summary>
    /// Retry loop that sends every attempt with the same idempotency key, so
    /// the server can replay a request that already went through.
    /// </summary>
    private async Task<JsonDocument> ExecuteWithRetryAsync(
        Func<string?, Task<HttpResponseMessage>> requestFunc,
        string? idempotencyKey,
        CancellationToken cancellationToken,
        bool retryUnknownOutcomes = true)
    {
        SendlyException? lastException = null;
        var waitedOut = false;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            if (attempt > 0 && !waitedOut)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                await Task.Delay(delay, cancellationToken);
            }
            waitedOut = false;

            try
            {
                var response = await requestFunc(idempotencyKey);
                return await HandleResponseAsync(response, cancellationToken);
            }
            catch (AuthenticationException) { throw; }
            catch (ValidationException) { throw; }
            catch (NotFoundException) { throw; }
            catch (InsufficientCreditsException) { throw; }
            catch (RateLimitException e) when (!WaitsOut(e)) { throw; }
            catch (RateLimitException e)
            {
                if (attempt < _maxRetries && e.RetryAfter is { } wait && wait > TimeSpan.Zero)
                {
                    await Task.Delay(wait, cancellationToken);
                    waitedOut = true;
                }
                lastException = e;
            }
            catch (SendlyException e) when (e.StatusCode is >= 400 and < 500 and not 408) { throw; }
            catch (SendlyException) when (!retryUnknownOutcomes) { throw; }
            catch (SendlyException e)
            {
                lastException = e;
            }
            catch (HttpRequestException e)
            {
                lastException = new NetworkException($"Request failed: {e.Message}", e);
                if (!retryUnknownOutcomes) throw lastException;
            }
            catch (TaskCanceledException e) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = new NetworkException("Request timed out", e);
                if (!retryUnknownOutcomes) throw lastException;
            }
        }

        throw lastException ?? new SendlyException("Request failed after retries");
    }

    /// <summary>
    /// Generates an idempotency key for a logical request. Reused across retry
    /// attempts so the server can recognize a retry of a timed-out POST that
    /// actually reached it.
    /// </summary>
    private static string GenerateIdempotencyKey()
    {
        return $"sendly-dotnet-retry-{Guid.NewGuid()}";
    }

    /// <summary>
    /// Validates and normalizes a caller-supplied idempotency key. Empty and
    /// whitespace-only values are treated as absent (auto-generation still
    /// applies); invalid values fail fast before any network call.
    /// </summary>
    private static string? NormalizeIdempotencyKey(string? key)
    {
        if (key == null) return null;

        var trimmed = key.Trim();
        if (trimmed.Length == 0) return null;

        if (trimmed.Length > 255 || trimmed.Any(c => c < 0x20 || c > 0x7E))
            throw new ValidationException("Idempotency key must be 1-255 printable ASCII characters");

        return trimmed;
    }

    private async Task<JsonDocument> HandleResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrEmpty(body))
                return JsonDocument.Parse("{}");

            try
            {
                return JsonDocument.Parse(body);
            }
            catch (JsonException)
            {
                var snippet = body.Length > 200 ? body.Substring(0, 200) : body;
                throw new SendlyException(
                    $"Expected JSON from the Sendly API but the response could not be parsed " +
                    $"(HTTP {(int)response.StatusCode}). Check that BaseUrl points at the API " +
                    $"(https://sendly.live/api/v1) and that no proxy is intercepting the request. " +
                    $"Response began: {snippet}",
                    (int)response.StatusCode);
            }
        }

        JsonDocument? errorDoc = null;
        string message = "Unknown error";
        string? apiErrorCode = null;
        int? bodyRetryAfter = null;
        JsonElement? responseBody = null;
        var fieldErrors = new List<SendlyFieldError>();

        try
        {
            errorDoc = JsonDocument.Parse(body);
            var root = errorDoc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
                responseBody = root.Clone();
            if (root.TryGetProperty("error", out var errProp) && errProp.ValueKind == JsonValueKind.String)
                apiErrorCode = errProp.GetString();
            if (root.TryGetProperty("retryAfter", out var retryProp) && retryProp.ValueKind == JsonValueKind.Number &&
                retryProp.TryGetInt32(out var retrySeconds))
                bodyRetryAfter = retrySeconds;
            if (root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                message = msgProp.GetString() ?? message;
            else if (apiErrorCode != null)
                message = apiErrorCode;
            if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in errorsProp.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object) continue;
                    var path = entry.TryGetProperty("path", out var pathProp) && pathProp.ValueKind == JsonValueKind.String
                        ? pathProp.GetString() ?? string.Empty
                        : string.Empty;
                    var detail = entry.TryGetProperty("message", out var detailProp) && detailProp.ValueKind == JsonValueKind.String
                        ? detailProp.GetString() ?? string.Empty
                        : string.Empty;
                    fieldErrors.Add(new SendlyFieldError(path, detail));
                }
            }
        }
        catch
        {
            message = body;
        }
        finally
        {
            errorDoc?.Dispose();
        }

        SendlyException exception = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new AuthenticationException(message),
            HttpStatusCode.PaymentRequired => new InsufficientCreditsException(message),
            HttpStatusCode.NotFound => new NotFoundException(message),
            HttpStatusCode.TooManyRequests => CreateRateLimitException(message, response, apiErrorCode, bodyRetryAfter),
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => new ValidationException(message, (int)response.StatusCode),
            _ => new SendlyException(message, (int)response.StatusCode)
        };
        exception.ApiErrorCode = apiErrorCode;
        exception.FieldErrors = fieldErrors;
        exception.ResponseBody = responseBody;
        throw exception;
    }

    private static RateLimitException CreateRateLimitException(string message, HttpResponseMessage response, string? apiErrorCode, int? bodyRetryAfter)
    {
        TimeSpan? retryAfter = bodyRetryAfter.HasValue ? TimeSpan.FromSeconds(bodyRetryAfter.Value) : null;

        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            var value = values.FirstOrDefault();
            if (int.TryParse(value, out var seconds))
            {
                retryAfter = TimeSpan.FromSeconds(seconds);
            }
        }

        return new RateLimitException(message, retryAfter, apiErrorCode);
    }

    /// <summary>
    /// Gets the JSON serializer options.
    /// </summary>
    internal JsonSerializerOptions JsonOptions => _jsonOptions;

    public void SetOrganizationId(string organizationId)
    {
        _httpClient.DefaultRequestHeaders.Remove("X-Organization-Id");
        if (!string.IsNullOrEmpty(organizationId))
            _httpClient.DefaultRequestHeaders.Add("X-Organization-Id", organizationId);
    }

    /// <summary>
    /// Disposes the client.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the client.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _httpClient.Dispose();
        }

        _disposed = true;
    }
}

/// <summary>
/// Configuration options for the Sendly client.
/// </summary>
public class SendlyClientOptions
{
    /// <summary>
    /// API base URL. Defaults to https://sendly.live/api/v1
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Request timeout. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum retry attempts. Defaults to 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    public string? OrganizationId { get; set; }
}
