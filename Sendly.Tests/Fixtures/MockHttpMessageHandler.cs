using System.Net;

namespace Sendly.Tests.Fixtures;

/// <summary>
/// Mock HttpMessageHandler for testing HTTP requests without making real network calls.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    private readonly List<HttpRequestMessage> _requests = new();

    /// <summary>
    /// Gets all requests that were made through this handler.
    /// </summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requests.AsReadOnly();

    /// <summary>
    /// Gets the last request that was made.
    /// </summary>
    public HttpRequestMessage? LastRequest => _requests.LastOrDefault();

    /// <summary>
    /// Queues a response to be returned for the next request.
    /// </summary>
    public void QueueResponse(HttpResponseMessage response)
    {
        _responses.Enqueue(response);
    }

    /// <summary>
    /// Queues a response with the specified status code and content.
    /// </summary>
    public void QueueResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
        };
        _responses.Enqueue(response);
    }

    /// <summary>
    /// Queues a successful response with the specified content.
    /// </summary>
    public void QueueSuccessResponse(string content)
    {
        QueueResponse(HttpStatusCode.OK, content);
    }

    /// <summary>
    /// Queues multiple responses to be returned in order.
    /// </summary>
    public void QueueResponses(params HttpResponseMessage[] responses)
    {
        foreach (var response in responses)
        {
            _responses.Enqueue(response);
        }
    }

    /// <summary>
    /// Clears all queued responses and recorded requests.
    /// </summary>
    public void Clear()
    {
        _responses.Clear();
        _requests.Clear();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Store a copy of the request for verification
        var requestCopy = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            requestCopy.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync(cancellationToken);
            requestCopy.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
        }
        _requests.Add(requestCopy);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No response queued. Call QueueResponse() before making requests.");
        }

        return await Task.FromResult(_responses.Dequeue());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var response in _responses)
            {
                response.Dispose();
            }
            _responses.Clear();

            foreach (var request in _requests)
            {
                request.Dispose();
            }
            _requests.Clear();
        }
        base.Dispose(disposing);
    }
}
