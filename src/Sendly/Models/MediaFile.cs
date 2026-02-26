using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Represents an uploaded media file.
/// </summary>
public class MediaFile
{
    /// <summary>
    /// Unique media file identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Public URL of the media file.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type.
    /// </summary>
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; set; }
}
