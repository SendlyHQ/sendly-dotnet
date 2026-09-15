using System.Text.Json;

namespace Sendly.Exceptions;

/// <summary>
/// Base exception for all Sendly errors.
/// </summary>
public class SendlyException : Exception
{
    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Error code.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// The machine-readable <c>error</c> string from the API response body
    /// (e.g. <c>rcs_not_enabled</c>, <c>rcs_field_locked</c>), or null when
    /// the response carried none. Unlike <see cref="ErrorCode"/>, which is a
    /// fixed per-exception-class constant, this tells apart the different
    /// reasons a route can answer with the same HTTP status.
    /// </summary>
    public string? ApiErrorCode { get; internal set; }

    /// <summary>
    /// Per-field problems from the API response body's <c>errors</c> array,
    /// when the response carried one (e.g. 422 <c>rcs_invalid_content</c>).
    /// Empty otherwise.
    /// </summary>
    public IReadOnlyList<SendlyFieldError> FieldErrors { get; internal set; } = Array.Empty<SendlyFieldError>();

    /// <summary>
    /// The JSON object the API answered with, or null when the error did not
    /// come from an API response (or the body was not a JSON object). Some
    /// refusals carry more than <c>error</c> and <c>message</c>: a 409
    /// <c>agent_in_use</c> lists the numbers the agent still answers under
    /// <c>numbers</c>, and a 422 <c>invalid_address</c> carries a corrected
    /// address (or null) under <c>suggested</c>.
    /// </summary>
    public JsonElement? ResponseBody { get; internal set; }

    /// <summary>
    /// Creates a new SendlyException.
    /// </summary>
    public SendlyException(string message, int statusCode = 0, string? errorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// One entry of an API response's <c>errors</c> array — the path of the
/// offending field and what is wrong with it.
/// </summary>
public class SendlyFieldError
{
    /// <summary>Dotted path of the field (e.g. <c>brand.ein</c>, <c>devices.0.phoneNumber</c>).</summary>
    public string Path { get; }

    /// <summary>What is wrong with the field.</summary>
    public string Message { get; }

    public SendlyFieldError(string path, string message)
    {
        Path = path;
        Message = message;
    }

    public override string ToString() => $"{Path}: {Message}";
}
