using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
    public const string Version = "1.0.5";

    /// <summary>
    /// Default API base URL.
    /// </summary>
    public const string DefaultBaseUrl = "https://sendly.live/api/v1";

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
    /// Gets the Templates resource.
    /// </summary>
    public TemplatesResource Templates { get; }

    /// <summary>
    /// Creates a new Sendly client.
    /// </summary>
    /// <param name="apiKey">Your Sendly API key</param>
    /// <param name="options">Optional client configuration</param>
    public SendlyClient(string apiKey, SendlyClientOptions? options = null)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new AuthenticationException("API key is required");

        _apiKey = apiKey;
        options ??= new SendlyClientOptions();
        _maxRetries = options.MaxRetries;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl ?? DefaultBaseUrl),
            Timeout = options.Timeout
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"sendly-dotnet/{Version}");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        Messages = new MessagesResource(this);
        Webhooks = new WebhooksResource(this);
        Account = new AccountResource(this);
        Verify = new VerifyResource(this);
        Templates = new TemplatesResource(this);
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
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        return await ExecuteWithRetryAsync(
            () => _httpClient.PostAsync(path, content, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Makes a PATCH request.
    /// </summary>
    internal async Task<JsonDocument> PatchAsync<T>(string path, T body, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        return await ExecuteWithRetryAsync(
            () => _httpClient.PatchAsync(path, content, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Makes a PUT request.
    /// </summary>
    internal async Task<JsonDocument> PutAsync<T>(string path, T body, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        return await ExecuteWithRetryAsync(
            () => _httpClient.PutAsync(path, content, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Makes a DELETE request.
    /// </summary>
    internal async Task<JsonDocument> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(
            () => _httpClient.DeleteAsync(path, cancellationToken),
            cancellationToken);
    }

    private string BuildUrl(string path, Dictionary<string, string>? queryParams)
    {
        if (queryParams == null || queryParams.Count == 0)
            return path;

        var query = string.Join("&", queryParams
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return string.IsNullOrEmpty(query) ? path : $"{path}?{query}";
    }

    private async Task<JsonDocument> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> requestFunc,
        CancellationToken cancellationToken)
    {
        SendlyException? lastException = null;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            if (attempt > 0)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                await Task.Delay(delay, cancellationToken);
            }

            try
            {
                var response = await requestFunc();
                return await HandleResponseAsync(response, cancellationToken);
            }
            catch (AuthenticationException) { throw; }
            catch (ValidationException) { throw; }
            catch (NotFoundException) { throw; }
            catch (InsufficientCreditsException) { throw; }
            catch (RateLimitException e)
            {
                if (e.RetryAfter.HasValue)
                {
                    await Task.Delay(e.RetryAfter.Value, cancellationToken);
                }
                lastException = e;
            }
            catch (SendlyException e)
            {
                lastException = e;
            }
            catch (HttpRequestException e)
            {
                lastException = new NetworkException($"Request failed: {e.Message}", e);
            }
            catch (TaskCanceledException e) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = new NetworkException("Request timed out", e);
            }
        }

        throw lastException ?? new SendlyException("Request failed after retries");
    }

    private async Task<JsonDocument> HandleResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return string.IsNullOrEmpty(body)
                ? JsonDocument.Parse("{}")
                : JsonDocument.Parse(body);
        }

        JsonDocument? errorDoc = null;
        string message = "Unknown error";

        try
        {
            errorDoc = JsonDocument.Parse(body);
            if (errorDoc.RootElement.TryGetProperty("message", out var msgProp))
                message = msgProp.GetString() ?? message;
            else if (errorDoc.RootElement.TryGetProperty("error", out var errProp))
                message = errProp.GetString() ?? message;
        }
        catch
        {
            message = body;
        }

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new AuthenticationException(message),
            HttpStatusCode.PaymentRequired => new InsufficientCreditsException(message),
            HttpStatusCode.NotFound => new NotFoundException(message),
            HttpStatusCode.TooManyRequests => CreateRateLimitException(message, response),
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => new ValidationException(message),
            _ => new SendlyException(message, (int)response.StatusCode)
        };
    }

    private static RateLimitException CreateRateLimitException(string message, HttpResponseMessage response)
    {
        TimeSpan? retryAfter = null;

        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            var value = values.FirstOrDefault();
            if (int.TryParse(value, out var seconds))
            {
                retryAfter = TimeSpan.FromSeconds(seconds);
            }
        }

        return new RateLimitException(message, retryAfter);
    }

    /// <summary>
    /// Gets the JSON serializer options.
    /// </summary>
    internal JsonSerializerOptions JsonOptions => _jsonOptions;

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
}
