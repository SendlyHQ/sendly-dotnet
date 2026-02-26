using System.Net.Http.Headers;
using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;

namespace Sendly.Resources;

/// <summary>
/// Resource for uploading media files for MMS.
/// </summary>
public class MediaResource
{
    private readonly SendlyClient _client;

    internal MediaResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Uploads a media file from a local file path.
    /// </summary>
    /// <param name="filePath">Path to the file on disk</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The uploaded media file</returns>
    public async Task<MediaFile> UploadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ValidationException("File path is required");

        if (!File.Exists(filePath))
            throw new ValidationException($"File not found: {filePath}");

        var fileName = Path.GetFileName(filePath);
        var contentType = GetContentType(fileName);

        using var stream = File.OpenRead(filePath);
        return await UploadAsync(stream, fileName, contentType, cancellationToken);
    }

    /// <summary>
    /// Uploads a media file from a stream.
    /// </summary>
    /// <param name="stream">File stream</param>
    /// <param name="fileName">File name with extension</param>
    /// <param name="contentType">MIME content type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The uploaded media file</returns>
    public async Task<MediaFile> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ValidationException("Stream is required");

        if (string.IsNullOrEmpty(fileName))
            throw new ValidationException("File name is required");

        if (string.IsNullOrEmpty(contentType))
            throw new ValidationException("Content type is required");

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        using var response = await _client.PostContentAsync("/media", content, cancellationToken);
        var root = response.RootElement;

        JsonElement data;
        if (root.TryGetProperty("data", out data))
        {
            return JsonSerializer.Deserialize<MediaFile>(data.GetRawText(), _client.JsonOptions)
                ?? new MediaFile();
        }

        return JsonSerializer.Deserialize<MediaFile>(root.GetRawText(), _client.JsonOptions)
            ?? new MediaFile();
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
