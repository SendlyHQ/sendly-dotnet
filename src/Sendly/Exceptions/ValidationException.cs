namespace Sendly.Exceptions;

/// <summary>
/// Thrown when the request contains invalid parameters.
/// </summary>
public class ValidationException : SendlyException
{
    /// <summary>
    /// Creates a new ValidationException.
    /// </summary>
    public ValidationException(string message = "Validation failed")
        : base(message, 400, "VALIDATION_ERROR")
    {
    }

    /// <summary>
    /// Creates a new ValidationException for a response with the given status (400 or 422).
    /// </summary>
    public ValidationException(string message, int statusCode)
        : base(message, statusCode, "VALIDATION_ERROR")
    {
    }
}
