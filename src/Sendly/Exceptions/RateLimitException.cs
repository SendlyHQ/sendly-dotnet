namespace Sendly.Exceptions;

/// <summary>
/// Thrown when the API answers 429. The client waits out and retries a 429 on
/// its own, up to <c>MaxRetries</c> times, only when its
/// <see cref="SendlyException.ApiErrorCode"/> is <c>rate_limit_exceeded</c>,
/// <c>provision_rate_limit</c>, <c>too_many_concurrent_verifications</c> or
/// absent, and only when
/// <see cref="RetryAfter"/> is 60 seconds or less. Any other 429 is thrown on
/// the first attempt with <see cref="RetryAfter"/> set when the API gave one.
/// <c>too_many_failed_key_attempts</c> means too many requests from this
/// address used a wrong API key for the account, so its keys, even a correct
/// one, are refused from this address until <see cref="RetryAfter"/> has
/// passed, which can be up to five minutes; find the requests that use a
/// wrong key rather than retrying.
/// </summary>
public class RateLimitException : SendlyException
{
    /// <summary>
    /// Time to wait before retrying.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Creates a new RateLimitException.
    /// </summary>
    public RateLimitException(string message = "Rate limit exceeded", TimeSpan? retryAfter = null)
        : base(message, 429, "RATE_LIMIT_EXCEEDED")
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Creates a new RateLimitException carrying the API's <c>error</c> code,
    /// such as <c>rate_limit_exceeded</c> or <c>too_many_failed_key_attempts</c>,
    /// as <see cref="SendlyException.ApiErrorCode"/>.
    /// </summary>
    public RateLimitException(string message, TimeSpan? retryAfter, string? apiErrorCode)
        : this(message, retryAfter)
    {
        ApiErrorCode = apiErrorCode;
    }
}
