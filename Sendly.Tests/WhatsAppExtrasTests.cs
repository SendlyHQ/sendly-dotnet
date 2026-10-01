using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;
using Sendly.Resources;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for the WhatsApp extras: sender profile photo, conversational
/// components and calling, adding a number by code, and the call channel.
/// Fixtures follow what the API handlers return.
/// </summary>
public class WhatsAppExtrasTests : IDisposable
{
    private const string Sender = "+15555550123";
    private const string EncodedSender = "%2B15555550123";
    private const string SignupId = "0b7c9a2e-5d41-4f3a-9e8b-1c2d3e4f5a6b";

    private const string ProfileJson = @"{
        ""phoneNumber"": ""+15555550123"",
        ""displayName"": ""Acme Bakery"",
        ""profilePhotoUrl"": ""https://cdn.example.com/acme.jpg"",
        ""category"": ""Food"",
        ""about"": ""Fresh every morning"",
        ""description"": null,
        ""email"": null,
        ""website"": null,
        ""address"": null
    }";

    private const string ComponentsJson = @"{
        ""phoneNumber"": ""+15555550123"",
        ""iceBreakers"": [""What are your hours?"", ""Do you deliver?""],
        ""commands"": [{ ""command"": ""menu"", ""description"": ""See today's menu"" }]
    }";

    private const string VerifyingSignupJson = @"{
        ""id"": ""0b7c9a2e-5d41-4f3a-9e8b-1c2d3e4f5a6b"",
        ""status"": ""verifying"",
        ""phoneNumber"": ""+15555550123"",
        ""businessAccountId"": ""104729384756"",
        ""failureReasons"": null,
        ""verificationMethod"": ""sms"",
        ""verificationAttemptsRemaining"": 5,
        ""updatedAt"": ""2026-10-01T10:00:00.000Z""
    }";

    private const string ActiveSignupJson = @"{
        ""id"": ""0b7c9a2e-5d41-4f3a-9e8b-1c2d3e4f5a6b"",
        ""status"": ""active"",
        ""phoneNumber"": ""+15555550123"",
        ""businessAccountId"": ""104729384756"",
        ""failureReasons"": null,
        ""updatedAt"": ""2026-10-01T10:02:00.000Z""
    }";

    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public WhatsAppExtrasTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler)
        {
            BaseAddress = new Uri("https://api.test.com")
        };

        _client = new SendlyClient("test_api_key");
        var httpClientField = typeof(SendlyClient).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance);
        httpClientField?.SetValue(_client, _httpClient);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _httpClient?.Dispose();
        _mockHandler?.Dispose();
    }

    private static HttpResponseMessage ErrorResponse(HttpStatusCode status, string json)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private void QueueUnknownOutcome(string kind)
    {
        switch (kind)
        {
            case "timeout":
                _mockHandler.QueueException(new TaskCanceledException("The request was canceled"));
                break;
            case "network":
                _mockHandler.QueueException(new HttpRequestException("Connection reset"));
                break;
            default:
                _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.RequestTimeout,
                    @"{""error"": ""request_timeout"", ""message"": ""Request timed out""}"));
                break;
        }
    }

    #region Senders list fields

    [Fact]
    public async Task SendersListAsync_ReadsAccountAndCallingFields()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""senders"": [
                {
                    ""phoneNumber"": ""+447700900123"",
                    ""displayName"": ""Acme Bakery"",
                    ""status"": ""active"",
                    ""qualityRating"": ""GREEN"",
                    ""businessAccountId"": ""104729384756"",
                    ""businessName"": ""Acme Bakery Ltd"",
                    ""callingEnabled"": true,
                    ""outboundCallingAllowed"": true,
                    ""createdAt"": ""2026-09-01T10:00:00.000Z""
                },
                {
                    ""phoneNumber"": ""+15555550123"",
                    ""displayName"": null,
                    ""status"": ""pending"",
                    ""qualityRating"": null,
                    ""businessAccountId"": null,
                    ""businessName"": null,
                    ""callingEnabled"": false,
                    ""outboundCallingAllowed"": false,
                    ""createdAt"": ""2026-09-02T10:00:00.000Z""
                }
            ]
        }");

        var result = await _client.WhatsApp.Senders.ListAsync();

        var active = result.Senders[0];
        Assert.Equal("104729384756", active.BusinessAccountId);
        Assert.Equal("Acme Bakery Ltd", active.BusinessName);
        Assert.True(active.CallingEnabled);
        Assert.True(active.OutboundCallingAllowed);

        var pending = result.Senders[1];
        Assert.Null(pending.BusinessAccountId);
        Assert.Null(pending.BusinessName);
        Assert.False(pending.CallingEnabled);
        Assert.False(pending.OutboundCallingAllowed);
    }

    #endregion

    #region Profile photo

    [Fact]
    public async Task UploadProfilePhotoAsync_Stream_PostsMultipartFileField()
    {
        _mockHandler.QueueSuccessResponse(ProfileJson);
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var profile = await _client.WhatsApp.Senders.UploadProfilePhotoAsync(
            Sender, stream, "logo.jpg", "image/jpeg");

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith($"/whatsapp/senders/{EncodedSender}/profile/photo", request.RequestUri!.ToString());
        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("name=file", body);
        Assert.Contains("filename=logo.jpg", body);
        Assert.Contains("Content-Type: image/jpeg", body);

        Assert.Equal(Sender, profile.PhoneNumber);
        Assert.Equal("https://cdn.example.com/acme.jpg", profile.ProfilePhotoUrl);
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_FilePath_SendsFileNameAndPngType()
    {
        _mockHandler.QueueSuccessResponse(ProfileJson);
        var dir = Path.Combine(Path.GetTempPath(), "sendly-wa-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "square.png");
        await File.WriteAllBytesAsync(path, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        try
        {
            await _client.WhatsApp.Senders.UploadProfilePhotoAsync(Sender, path);
        }
        finally
        {
            Directory.Delete(dir, true);
        }

        var body = await _mockHandler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("name=file", body);
        Assert.Contains("filename=square.png", body);
        Assert.Contains("Content-Type: image/png", body);
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_MissingStream_ThrowsBeforeSending()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Senders.UploadProfilePhotoAsync(Sender, (Stream)null!, "logo.jpg", "image/jpeg"));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_MissingFile_ThrowsBeforeSending()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Senders.UploadProfilePhotoAsync(Sender, "/no/such/photo.jpg"));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_InvalidPhone_ThrowsBeforeSending()
    {
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Senders.UploadProfilePhotoAsync("15555550123", stream, "logo.jpg", "image/jpeg"));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_TooLarge_ThrowsOnFirstAttempt()
    {
        _mockHandler.QueueResponse(ErrorResponse((HttpStatusCode)413,
            @"{""error"": ""whatsapp_profile_photo_too_large"", ""message"": ""The photo must be 5 MB or smaller.""}"));
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

        var ex = await Assert.ThrowsAsync<SendlyException>(
            () => _client.WhatsApp.Senders.UploadProfilePhotoAsync(Sender, stream, "logo.jpg", "image/jpeg"));

        Assert.Equal(413, ex.StatusCode);
        Assert.Equal("whatsapp_profile_photo_too_large", ex.ApiErrorCode);
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_NotJpegOrPng_ThrowsValidationException()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.BadRequest,
            @"{""error"": ""whatsapp_profile_photo_invalid"", ""message"": ""The photo must be a JPEG or PNG image.""}"));
        using var stream = new MemoryStream(new byte[] { 0x47, 0x49, 0x46 });

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Senders.UploadProfilePhotoAsync(Sender, stream, "logo.gif", "image/gif"));

        Assert.Equal("whatsapp_profile_photo_invalid", ex.ApiErrorCode);
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_ServerError_IsNotRetried()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.BadGateway,
            @"{""error"": ""whatsapp_profile_update_failed"", ""message"": ""The photo couldn't be uploaded. WhatsApp needs a square JPEG or PNG at least 192 pixels wide. Please try again shortly.""}"));
        _mockHandler.QueueSuccessResponse(ProfileJson);
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var ex = await Assert.ThrowsAsync<SendlyException>(
            () => _client.WhatsApp.Senders.UploadProfilePhotoAsync(Sender, stream, "logo.jpg", "image/jpeg"));

        Assert.Equal(502, ex.StatusCode);
        Assert.Equal("whatsapp_profile_update_failed", ex.ApiErrorCode);
        Assert.Single(_mockHandler.Requests);
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("network")]
    [InlineData("408")]
    public async Task UploadProfilePhotoAsync_UnknownOutcome_IsNotRetried(string kind)
    {
        QueueUnknownOutcome(kind);
        _mockHandler.QueueSuccessResponse(ProfileJson);
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        await Assert.ThrowsAnyAsync<SendlyException>(
            () => _client.WhatsApp.Senders.UploadProfilePhotoAsync(Sender, stream, "logo.jpg", "image/jpeg"));

        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_NonSeekableStream_ResendsThePhotoOnRetry()
    {
        var handler = new StreamingHandler();
        var limited = ErrorResponse(HttpStatusCode.TooManyRequests,
            @"{""error"": ""rate_limit_exceeded"", ""message"": ""Too many requests"", ""retryAfter"": 1}");
        limited.Headers.Add("Retry-After", "1");
        handler.Responses.Enqueue(limited);
        handler.Responses.Enqueue(ErrorResponse(HttpStatusCode.OK, ProfileJson));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.test.com") };
        using var client = new SendlyClient("test_api_key", new SendlyClientOptions { MaxRetries = 1 });
        typeof(SendlyClient).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(client, httpClient);
        var photo = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x50, 0x48, 0x4F, 0x54, 0x4F };
        using var stream = new ForwardOnlyStream(photo);

        var profile = await client.WhatsApp.Senders.UploadProfilePhotoAsync(Sender, stream, "logo.jpg", "image/jpeg");

        Assert.Equal(2, handler.Bodies.Count);
        Assert.All(handler.Bodies, body => Assert.Contains("PHOTO", body));
        Assert.Equal("https://cdn.example.com/acme.jpg", profile.ProfilePhotoUrl);
    }

    private sealed class StreamingHandler : HttpMessageHandler
    {
        public Queue<HttpResponseMessage> Responses { get; } = new();
        public List<string> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var sink = new MemoryStream();
            await request.Content!.CopyToAsync(sink, cancellationToken);
            Bodies.Add(System.Text.Encoding.Latin1.GetString(sink.ToArray()));
            return Responses.Dequeue();
        }
    }

    private sealed class ForwardOnlyStream : Stream
    {
        private readonly MemoryStream _inner;

        public ForwardOnlyStream(byte[] bytes) => _inner = new MemoryStream(bytes);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public async Task DeleteProfilePhotoAsync_SendsDeleteAndReturnsProfile()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""phoneNumber"": ""+15555550123"",
            ""displayName"": ""Acme Bakery"",
            ""profilePhotoUrl"": null,
            ""category"": null,
            ""about"": null,
            ""description"": null,
            ""email"": null,
            ""website"": null,
            ""address"": null
        }");

        var profile = await _client.WhatsApp.Senders.DeleteProfilePhotoAsync(Sender);

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.EndsWith($"/whatsapp/senders/{EncodedSender}/profile/photo", request.RequestUri!.ToString());
        Assert.Null(profile.ProfilePhotoUrl);
        Assert.Equal("Acme Bakery", profile.DisplayName);
    }

    [Fact]
    public async Task DeleteProfilePhotoAsync_InvalidPhone_ThrowsBeforeSending()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Senders.DeleteProfilePhotoAsync("invalid"));
        Assert.Empty(_mockHandler.Requests);
    }

    #endregion

    #region Conversational components

    [Fact]
    public async Task GetConversationalComponentsAsync_ReadsIceBreakersAndCommands()
    {
        _mockHandler.QueueSuccessResponse(ComponentsJson);

        var components = await _client.WhatsApp.Senders.GetConversationalComponentsAsync(Sender);

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.EndsWith($"/whatsapp/senders/{EncodedSender}/conversational_components", request.RequestUri!.ToString());
        Assert.Equal(Sender, components.PhoneNumber);
        Assert.Equal(new[] { "What are your hours?", "Do you deliver?" }, components.IceBreakers);
        var command = Assert.Single(components.Commands);
        Assert.Equal("menu", command.Command);
        Assert.Equal("See today's menu", command.Description);
    }

    [Fact]
    public async Task UpdateConversationalComponentsAsync_SendsBothListsInCamelCase()
    {
        _mockHandler.QueueSuccessResponse(ComponentsJson);

        var components = await _client.WhatsApp.Senders.UpdateConversationalComponentsAsync(Sender,
            new UpdateWhatsAppConversationalComponentsRequest
            {
                IceBreakers = new() { "What are your hours?", "Do you deliver?" },
                Commands = new() { new WhatsAppCommand { Command = "menu", Description = "See today's menu" } }
            });

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.EndsWith($"/whatsapp/senders/{EncodedSender}/conversational_components", request.RequestUri!.ToString());
        using var doc = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal(2, root.GetProperty("iceBreakers").GetArrayLength());
        var sent = root.GetProperty("commands")[0];
        Assert.Equal("menu", sent.GetProperty("command").GetString());
        Assert.Equal("See today's menu", sent.GetProperty("description").GetString());
        Assert.Equal(2, root.EnumerateObject().Count());
        Assert.Equal("menu", components.Commands[0].Command);
    }

    [Fact]
    public async Task UpdateConversationalComponentsAsync_LeavesOutUnsetListAndSendsEmptyListToClear()
    {
        _mockHandler.QueueSuccessResponse(@"{""phoneNumber"": ""+15555550123"", ""iceBreakers"": [], ""commands"": []}");

        var components = await _client.WhatsApp.Senders.UpdateConversationalComponentsAsync(Sender,
            new UpdateWhatsAppConversationalComponentsRequest { IceBreakers = new() });

        var body = await _mockHandler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Equal(@"{""iceBreakers"":[]}", body);
        Assert.Empty(components.IceBreakers);
        Assert.Empty(components.Commands);
    }

    [Fact]
    public async Task UpdateConversationalComponentsAsync_NullRequest_ThrowsBeforeSending()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Senders.UpdateConversationalComponentsAsync(Sender, null!));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task UpdateConversationalComponentsAsync_Invalid_ThrowsValidationExceptionWithMessage()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.BadRequest,
            @"{""error"": ""invalid_request"", ""message"": ""At most 4 ice breakers are allowed.""}"));

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Senders.UpdateConversationalComponentsAsync(Sender,
                new UpdateWhatsAppConversationalComponentsRequest { IceBreakers = new() { "a", "b", "c", "d", "e" } }));

        Assert.Equal("invalid_request", ex.ApiErrorCode);
        Assert.Equal("At most 4 ice breakers are allowed.", ex.Message);
    }

    #endregion

    #region Calling

    [Fact]
    public async Task SetCallingAsync_SendsEnabledAndReadsSettings()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""phoneNumber"": ""+15555550123"",
            ""callingEnabled"": true,
            ""outboundCallingAllowed"": false
        }");

        var settings = await _client.WhatsApp.Senders.SetCallingAsync(Sender, true);

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.EndsWith($"/whatsapp/senders/{EncodedSender}/calling", request.RequestUri!.ToString());
        Assert.Equal(@"{""enabled"":true}", await request.Content!.ReadAsStringAsync());
        Assert.Equal(Sender, settings.PhoneNumber);
        Assert.True(settings.CallingEnabled);
        Assert.False(settings.OutboundCallingAllowed);
    }

    [Fact]
    public async Task SetCallingAsync_Disable_SendsFalse()
    {
        _mockHandler.QueueSuccessResponse(@"{""phoneNumber"": ""+15555550123"", ""callingEnabled"": false, ""outboundCallingAllowed"": false}");

        await _client.WhatsApp.Senders.SetCallingAsync(Sender, false);

        Assert.Equal(@"{""enabled"":false}", await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task SetCallingAsync_VoiceNotEnabled_ThrowsOnFirstAttempt()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.Conflict,
            @"{""error"": ""voice_not_enabled"", ""message"": ""Turn on calls for this number first, in its voice settings, so WhatsApp calls have somewhere to ring.""}"));

        var ex = await Assert.ThrowsAsync<SendlyException>(
            () => _client.WhatsApp.Senders.SetCallingAsync(Sender, true));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("voice_not_enabled", ex.ApiErrorCode);
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task SetCallingAsync_MetaRefused_ThrowsValidationException()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.UnprocessableEntity,
            @"{""error"": ""whatsapp_calling_unavailable"", ""message"": ""WhatsApp didn't allow calling on this number.""}"));

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Senders.SetCallingAsync(Sender, true));

        Assert.Equal(422, ex.StatusCode);
        Assert.Equal("whatsapp_calling_unavailable", ex.ApiErrorCode);
    }

    #endregion

    #region Signup by code

    [Fact]
    public async Task SignupCreateAsync_WithBusinessAccount_SendsCamelCaseFieldsAndReadsVerifyingSession()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.Created,
            VerifyingSignupJson.Replace(@"""sms""", @"""voice""")));

        var signup = await _client.WhatsApp.Signup.CreateAsync(new StartWhatsAppSignupRequest
        {
            PhoneNumber = Sender,
            BusinessAccountId = "104729384756",
            VerificationMethod = WhatsAppVerificationMethod.Voice,
            DisplayName = "Acme Bakery"
        });

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/whatsapp/signup", request.RequestUri!.ToString());
        using var doc = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal(Sender, root.GetProperty("phoneNumber").GetString());
        Assert.Equal("104729384756", root.GetProperty("businessAccountId").GetString());
        Assert.Equal("voice", root.GetProperty("verificationMethod").GetString());
        Assert.Equal("Acme Bakery", root.GetProperty("displayName").GetString());

        Assert.Equal(SignupId, signup.Id);
        Assert.Equal("verifying", signup.Status);
        Assert.Equal(string.Empty, signup.ConnectUrl);
        Assert.Equal(Sender, signup.PhoneNumber);
        Assert.Equal("104729384756", signup.BusinessAccountId);
        Assert.Equal("voice", signup.VerificationMethod);
        Assert.Equal(5, signup.VerificationAttemptsRemaining);
        Assert.Null(signup.FailureReasons);
        Assert.Equal("2026-10-01T10:00:00.000Z", signup.UpdatedAt);
    }

    [Fact]
    public async Task SignupCreateAsync_RequestWithoutBusinessAccount_SendsOnlyPhoneNumber()
    {
        _mockHandler.QueueSuccessResponse(@"{""id"": ""was_123"", ""connectUrl"": ""https://sendly.live/whatsapp/connect?token=tok"", ""status"": ""initiated""}");

        var signup = await _client.WhatsApp.Signup.CreateAsync(new StartWhatsAppSignupRequest { PhoneNumber = Sender });

        using var doc = JsonDocument.Parse(await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
        var sent = Assert.Single(doc.RootElement.EnumerateObject());
        Assert.Equal("phoneNumber", sent.Name);
        Assert.Equal(Sender, sent.Value.GetString());
        Assert.Equal("https://sendly.live/whatsapp/connect?token=tok", signup.ConnectUrl);
        Assert.Null(signup.VerificationMethod);
        Assert.Null(signup.VerificationAttemptsRemaining);
    }

    [Fact]
    public async Task SignupCreateAsync_NullRequest_ThrowsBeforeSending()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Signup.CreateAsync((StartWhatsAppSignupRequest)null!));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task SignupCreateAsync_RequestWithInvalidPhone_ThrowsBeforeSending()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Signup.CreateAsync(new StartWhatsAppSignupRequest
            {
                PhoneNumber = "5555550123",
                BusinessAccountId = "104729384756"
            }));
        Assert.Empty(_mockHandler.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SignupCreateAsync_RequestWithBlankBusinessAccountId_ThrowsBeforeSending(string businessAccountId)
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Signup.CreateAsync(new StartWhatsAppSignupRequest
            {
                PhoneNumber = Sender,
                BusinessAccountId = businessAccountId
            }));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task SignupCreateAsync_BusinessAccountNotFound_ThrowsNotFound()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.NotFound,
            @"{""error"": ""whatsapp_business_account_not_found"", ""message"": ""There's no connected WhatsApp Business account with this id in your workspace.""}"));

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => _client.WhatsApp.Signup.CreateAsync(new StartWhatsAppSignupRequest
            {
                PhoneNumber = Sender,
                BusinessAccountId = "999"
            }));

        Assert.Equal("whatsapp_business_account_not_found", ex.ApiErrorCode);
    }

    [Fact]
    public async Task SignupCreateAsync_VerificationStartUnreachable_ThrowsWithoutStartingAnotherSession()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.BadGateway,
            @"{""error"": ""whatsapp_verification_start_failed"", ""message"": ""WhatsApp couldn't start verifying this number. Any setup fee is refunded automatically. Please try again shortly.""}"));
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.Created, VerifyingSignupJson));

        var ex = await Assert.ThrowsAsync<SendlyException>(
            () => _client.WhatsApp.Signup.CreateAsync(new StartWhatsAppSignupRequest
            {
                PhoneNumber = Sender,
                BusinessAccountId = "104729384756"
            }));

        Assert.Equal(502, ex.StatusCode);
        Assert.Equal("whatsapp_verification_start_failed", ex.ApiErrorCode);
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task SignupCreateAsync_FacebookUnavailable_IsStillRetriedUnderTheSameKey()
    {
        var unavailable = ErrorResponse(HttpStatusCode.ServiceUnavailable,
            @"{""error"": ""whatsapp_unavailable"", ""message"": ""WhatsApp connections are temporarily unavailable. You haven't been charged. Please try again later."", ""retryAfter"": 3600}");
        unavailable.Headers.Add("Retry-After", "3600");
        _mockHandler.QueueResponse(unavailable);
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.Created,
            @"{""id"": ""0b7c9a2e-5d41-4f3a-9e8b-1c2d3e4f5a6b"", ""connectUrl"": ""https://sendly.live/whatsapp/connect/tok_abc"", ""status"": ""initiated""}"));

        var session = await _client.WhatsApp.Signup.CreateAsync(Sender);

        Assert.Equal("initiated", session.Status);
        Assert.Equal(2, _mockHandler.Requests.Count);
        Assert.Equal(
            _mockHandler.Requests[0].Headers.GetValues("Idempotency-Key").Single(),
            _mockHandler.Requests[1].Headers.GetValues("Idempotency-Key").Single());
    }

    [Fact]
    public async Task SignupCreateAsync_FacebookPath_ServerErrorIsRetriedWhateverItsCode()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.BadGateway,
            @"{""error"": ""whatsapp_verification_start_failed"", ""message"": ""WhatsApp couldn't start verifying this number.""}"));
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.Created,
            @"{""id"": ""0b7c9a2e-5d41-4f3a-9e8b-1c2d3e4f5a6b"", ""connectUrl"": ""https://sendly.live/whatsapp/connect?token=tok_abc"", ""status"": ""initiated""}"));

        var session = await _client.WhatsApp.Signup.CreateAsync(Sender);

        Assert.Equal("initiated", session.Status);
        Assert.Equal(2, _mockHandler.Requests.Count);
    }

    [Fact]
    public async Task SignupCreateAsync_WithBusinessAccount_ServerErrorIsNotRetried()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.InternalServerError,
            @"{""error"": ""internal_error"", ""message"": ""Something went wrong asking WhatsApp for the code. Any setup fee is refunded automatically. Please try again.""}"));
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.Created, VerifyingSignupJson));

        var ex = await Assert.ThrowsAsync<SendlyException>(
            () => _client.WhatsApp.Signup.CreateAsync(new StartWhatsAppSignupRequest
            {
                PhoneNumber = Sender,
                BusinessAccountId = "104729384756"
            }));

        Assert.Equal(500, ex.StatusCode);
        Assert.Equal("internal_error", ex.ApiErrorCode);
        Assert.Single(_mockHandler.Requests);
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("network")]
    [InlineData("408")]
    public async Task SignupCreateAsync_WithBusinessAccount_UnknownOutcomeIsNotRetried(string kind)
    {
        QueueUnknownOutcome(kind);
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.Created, VerifyingSignupJson));

        await Assert.ThrowsAnyAsync<SendlyException>(
            () => _client.WhatsApp.Signup.CreateAsync(new StartWhatsAppSignupRequest
            {
                PhoneNumber = Sender,
                BusinessAccountId = "104729384756"
            }));

        Assert.Single(_mockHandler.Requests);
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("network")]
    public async Task SignupCreateAsync_RequestWithoutBusinessAccount_UnknownOutcomeIsStillRetried(string kind)
    {
        QueueUnknownOutcome(kind);
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.Created,
            @"{""id"": ""0b7c9a2e-5d41-4f3a-9e8b-1c2d3e4f5a6b"", ""connectUrl"": ""https://sendly.live/whatsapp/connect/tok_abc"", ""status"": ""initiated""}"));

        var session = await _client.WhatsApp.Signup.CreateAsync(new StartWhatsAppSignupRequest { PhoneNumber = Sender });

        Assert.Equal("initiated", session.Status);
        Assert.Equal(2, _mockHandler.Requests.Count);
    }

    [Theory]
    [InlineData(null)]
    public async Task SignupCreateAsync_RequestWithoutBusinessAccount_ServerErrorIsStillRetried(string? businessAccountId)
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.BadGateway,
            @"{""error"": ""server_error"", ""message"": ""Something went wrong. Please try again.""}"));
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.Created,
            @"{""id"": ""0b7c9a2e-5d41-4f3a-9e8b-1c2d3e4f5a6b"", ""connectUrl"": ""https://sendly.live/whatsapp/connect/tok_abc"", ""status"": ""initiated""}"));

        var session = await _client.WhatsApp.Signup.CreateAsync(new StartWhatsAppSignupRequest
        {
            PhoneNumber = Sender,
            BusinessAccountId = businessAccountId
        });

        Assert.Equal("initiated", session.Status);
        Assert.Equal(2, _mockHandler.Requests.Count);
    }

    [Fact]
    public async Task SignupGetAsync_WhileVerifying_ReadsVerificationFields()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""id"": ""0b7c9a2e-5d41-4f3a-9e8b-1c2d3e4f5a6b"",
            ""status"": ""verifying"",
            ""phoneNumber"": ""+15555550123"",
            ""businessAccountId"": ""104729384756"",
            ""failureReasons"": null,
            ""verificationMethod"": ""voice"",
            ""verificationAttemptsRemaining"": 3,
            ""updatedAt"": ""2026-10-01T10:00:00.000Z"",
            ""verificationCode"": ""482913""
        }");

        var signup = await _client.WhatsApp.Signup.GetAsync(SignupId);

        Assert.Equal("verifying", signup.Status);
        Assert.Equal("104729384756", signup.BusinessAccountId);
        Assert.Equal("voice", signup.VerificationMethod);
        Assert.Equal(3, signup.VerificationAttemptsRemaining);
        Assert.Equal("482913", signup.VerificationCode);
    }

    [Fact]
    public async Task SignupGetAsync_WhenActive_LeavesVerificationFieldsNull()
    {
        _mockHandler.QueueSuccessResponse(ActiveSignupJson);

        var signup = await _client.WhatsApp.Signup.GetAsync(SignupId);

        Assert.Equal("active", signup.Status);
        Assert.Null(signup.VerificationMethod);
        Assert.Null(signup.VerificationAttemptsRemaining);
        Assert.Null(signup.VerificationCode);
    }

    [Fact]
    public async Task SignupVerifyAsync_PostsCodeAndReturnsActiveSignup()
    {
        _mockHandler.QueueSuccessResponse(ActiveSignupJson);

        var signup = await _client.WhatsApp.Signup.VerifyAsync(SignupId, "482 913");

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith($"/whatsapp/signup/{SignupId}/verify", request.RequestUri!.ToString());
        Assert.Equal(@"{""code"":""482 913""}", await request.Content!.ReadAsStringAsync());
        Assert.Equal("active", signup.Status);
        Assert.Equal("104729384756", signup.BusinessAccountId);
    }

    [Fact]
    public async Task SignupVerifyAsync_WrongCode_ThrowsWithAttemptsRemaining()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.UnprocessableEntity,
            @"{""error"": ""whatsapp_verification_code_invalid"", ""message"": ""That code wasn't accepted. Check it, or request a new one."", ""attemptsRemaining"": 3}"));

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Signup.VerifyAsync(SignupId, "000000"));

        Assert.Equal(422, ex.StatusCode);
        Assert.Equal("whatsapp_verification_code_invalid", ex.ApiErrorCode);
        Assert.Equal(3, ex.ResponseBody!.Value.GetProperty("attemptsRemaining").GetInt32());
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task SignupVerifyAsync_TooManyWrongCodes_ThrowsOnFirstAttempt()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.Conflict,
            @"{""error"": ""whatsapp_verification_failed"", ""message"": ""Too many wrong codes.""}"));

        var ex = await Assert.ThrowsAsync<SendlyException>(
            () => _client.WhatsApp.Signup.VerifyAsync(SignupId, "000000"));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("whatsapp_verification_failed", ex.ApiErrorCode);
        Assert.Single(_mockHandler.Requests);
    }

    [Theory]
    [InlineData("whatsapp_activation_pending", "WhatsApp accepted the code, but we couldn't finish connecting the number. Our team has been alerted; check back shortly.")]
    [InlineData("whatsapp_verification_unavailable", "WhatsApp couldn't check the code right now. Please try again shortly.")]
    public async Task SignupVerifyAsync_ServerError_IsNotRetried(string code, string message)
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.BadGateway,
            @"{""error"": """ + code + @""", ""message"": """ + message + @"""}"));
        _mockHandler.QueueSuccessResponse(ActiveSignupJson);

        var ex = await Assert.ThrowsAsync<SendlyException>(
            () => _client.WhatsApp.Signup.VerifyAsync(SignupId, "482913"));

        Assert.Equal(502, ex.StatusCode);
        Assert.Equal(code, ex.ApiErrorCode);
        Assert.Single(_mockHandler.Requests);
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("network")]
    [InlineData("408")]
    public async Task SignupVerifyAsync_UnknownOutcome_IsNotRetried(string kind)
    {
        QueueUnknownOutcome(kind);
        _mockHandler.QueueSuccessResponse(ActiveSignupJson);

        await Assert.ThrowsAnyAsync<SendlyException>(
            () => _client.WhatsApp.Signup.VerifyAsync(SignupId, "482913"));

        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task SignupVerifyAsync_RateLimited_IsStillWaitedOutAndRetried()
    {
        var limited = ErrorResponse(HttpStatusCode.TooManyRequests,
            @"{""error"": ""rate_limit_exceeded"", ""message"": ""Too many requests"", ""retryAfter"": 1}");
        limited.Headers.Add("Retry-After", "1");
        _mockHandler.QueueResponse(limited);
        _mockHandler.QueueSuccessResponse(ActiveSignupJson);

        var signup = await _client.WhatsApp.Signup.VerifyAsync(SignupId, "482913");

        Assert.Equal("active", signup.Status);
        Assert.Equal(2, _mockHandler.Requests.Count);
    }

    private static Func<Task> CallThatIsNeverRetried(SendlyClient client, string call, Stream photo) => call switch
    {
        "verify" => () => client.WhatsApp.Signup.VerifyAsync(SignupId, "482913"),
        "create" => () => client.WhatsApp.Signup.CreateAsync(new StartWhatsAppSignupRequest { PhoneNumber = Sender, BusinessAccountId = "104729384756" }),
        _ => () => client.WhatsApp.Senders.UploadProfilePhotoAsync(Sender, photo, "logo.jpg", "image/jpeg"),
    };

    [Theory]
    [InlineData("verify")]
    [InlineData("create")]
    [InlineData("photo")]
    public async Task UnknownOutcome_SuccessStatusWithUnparseableBody_IsNotRetried(string call)
    {
        _mockHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>gateway</html>", System.Text.Encoding.UTF8, "text/html")
        });
        _mockHandler.QueueSuccessResponse(call == "photo" ? ProfileJson : ActiveSignupJson);
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        await Assert.ThrowsAnyAsync<SendlyException>(CallThatIsNeverRetried(_client, call, stream));

        Assert.Single(_mockHandler.Requests);
    }

    [Theory]
    [InlineData("verify")]
    [InlineData("create")]
    [InlineData("photo")]
    public async Task UnknownOutcome_UnfollowedRedirect_IsNotRetried(string call)
    {
        var redirect = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect)
        {
            Content = new StringContent("<html>moved</html>", System.Text.Encoding.UTF8, "text/html")
        };
        redirect.Headers.Location = new Uri("http://sendly.live/api/v1/whatsapp/signup");
        _mockHandler.QueueResponse(redirect);
        _mockHandler.QueueSuccessResponse(call == "photo" ? ProfileJson : ActiveSignupJson);
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var ex = await Assert.ThrowsAnyAsync<SendlyException>(CallThatIsNeverRetried(_client, call, stream));

        Assert.Equal(307, ex.StatusCode);
        Assert.Single(_mockHandler.Requests);
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("network")]
    public async Task SignupResendAsync_UnknownOutcome_IsStillRetried(string kind)
    {
        QueueUnknownOutcome(kind);
        _mockHandler.QueueSuccessResponse(VerifyingSignupJson);

        await _client.WhatsApp.Signup.ResendAsync(SignupId);

        Assert.Equal(2, _mockHandler.Requests.Count);
    }

    [Fact]
    public async Task SignupResendAsync_ServerError_IsStillRetried()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.BadGateway,
            @"{""error"": ""whatsapp_verification_resend_failed"", ""message"": ""WhatsApp couldn't send another code right now. Please try again shortly.""}"));
        _mockHandler.QueueSuccessResponse(VerifyingSignupJson);

        var signup = await _client.WhatsApp.Signup.ResendAsync(SignupId);

        Assert.Equal("verifying", signup.Status);
        Assert.Equal(2, _mockHandler.Requests.Count);
    }

    [Theory]
    [InlineData("", "482913")]
    [InlineData("0b7c9a2e", "")]
    public async Task SignupVerifyAsync_MissingIdOrCode_ThrowsBeforeSending(string id, string code)
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Signup.VerifyAsync(id, code));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task SignupResendAsync_WithMethod_SendsVerificationMethod()
    {
        _mockHandler.QueueSuccessResponse(VerifyingSignupJson.Replace(@"""sms""", @"""voice"""));

        var signup = await _client.WhatsApp.Signup.ResendAsync(SignupId, WhatsAppVerificationMethod.Voice);

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith($"/whatsapp/signup/{SignupId}/resend", request.RequestUri!.ToString());
        Assert.Equal(@"{""verificationMethod"":""voice""}", await request.Content!.ReadAsStringAsync());
        Assert.Equal("verifying", signup.Status);
        Assert.Equal("voice", signup.VerificationMethod);
    }

    [Fact]
    public async Task SignupResendAsync_WithoutMethod_SendsEmptyBody()
    {
        _mockHandler.QueueSuccessResponse(VerifyingSignupJson);

        await _client.WhatsApp.Signup.ResendAsync(SignupId);

        Assert.Equal("{}", await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task SignupResendAsync_TooSoon_ThrowsRateLimitAtOnceWithRetryAfter()
    {
        var response = ErrorResponse(HttpStatusCode.TooManyRequests,
            @"{""error"": ""whatsapp_verification_resend_too_soon"", ""message"": ""Wait 30 seconds before requesting another code."", ""retryAfter"": 30}");
        response.Headers.Add("Retry-After", "30");
        _mockHandler.QueueResponse(response);

        var ex = await Assert.ThrowsAsync<RateLimitException>(
            () => _client.WhatsApp.Signup.ResendAsync(SignupId));

        Assert.Equal("whatsapp_verification_resend_too_soon", ex.ApiErrorCode);
        Assert.Equal(TimeSpan.FromSeconds(30), ex.RetryAfter);
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task SignupResendAsync_MissingId_ThrowsBeforeSending()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Signup.ResendAsync(""));
        Assert.Empty(_mockHandler.Requests);
    }

    #endregion

    #region Dropped connection

    [Theory]
    [InlineData("verify", "POST /api/v1/whatsapp/signup/0b7c9a2e-5d41-4f3a-9e8b-1c2d3e4f5a6b/verify")]
    [InlineData("create", "POST /api/v1/whatsapp/signup")]
    [InlineData("photo", "POST /api/v1/whatsapp/senders/%2B15555550123/profile/photo")]
    public async Task ReusedConnectionClosedAfterTheRequest_IsNotSentAgainByTheHttpStack(string call, string request)
    {
        using var server = new DroppingServer(call == "photo" ? ProfileJson : ActiveSignupJson);
        using var client = new SendlyClient("test_api_key", new SendlyClientOptions
        {
            BaseUrl = server.BaseUrl,
            MaxRetries = 0,
            Timeout = TimeSpan.FromSeconds(10)
        });
        await client.WhatsApp.Senders.ListAsync();
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var ex = await Assert.ThrowsAsync<NetworkException>(CallThatIsNeverRetried(client, call, stream));

        var dropped = Assert.IsType<HttpRequestException>(ex.InnerException);
        Assert.Equal(HttpRequestError.ResponseEnded, dropped.HttpRequestError);
        Assert.Equal(new[] { "1 GET /api/v1/whatsapp/senders", "1 " + request }, server.Requests);
    }

    private sealed class DroppingServer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly ConcurrentQueue<string> _requests = new();
        private readonly ConcurrentBag<TcpClient> _connections = new();
        private readonly string _laterBody;
        private int _requestCount;

        public DroppingServer(string laterBody)
        {
            _laterBody = laterBody;
            _listener.Start();
            _ = AcceptAsync();
        }

        public string BaseUrl => $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/api/v1";

        public string[] Requests => _requests.ToArray();

        public void Dispose()
        {
            _listener.Stop();
            foreach (var connection in _connections)
                connection.Dispose();
        }

        private async Task AcceptAsync()
        {
            for (var number = 1; ; number++)
            {
                TcpClient connection;
                try
                {
                    connection = await _listener.AcceptTcpClientAsync();
                }
                catch (Exception e) when (e is SocketException or ObjectDisposedException)
                {
                    return;
                }
                _connections.Add(connection);
                _ = ServeAsync(connection, number);
            }
        }

        private async Task ServeAsync(TcpClient connection, int number)
        {
            try
            {
                using (connection)
                {
                    var stream = connection.GetStream();
                    while (await ReadRequestAsync(stream) is { } line)
                    {
                        _requests.Enqueue($"{number} {line}");
                        var count = Interlocked.Increment(ref _requestCount);
                        if (count == 2)
                            return;
                        await RespondAsync(stream, count == 1 ? @"{""senders"":[]}" : _laterBody);
                    }
                }
            }
            catch (Exception e) when (e is IOException or ObjectDisposedException)
            {
            }
        }

        private static async Task<string?> ReadRequestAsync(NetworkStream stream)
        {
            var received = new List<byte>();
            var buffer = new byte[8192];
            int headerLength;
            while ((headerLength = HeaderLength(received)) < 0)
            {
                var read = await stream.ReadAsync(buffer);
                if (read == 0)
                    return null;
                received.AddRange(buffer.Take(read));
            }

            var head = System.Text.Encoding.ASCII.GetString(received.ToArray(), 0, headerLength).Split("\r\n");
            var contentLength = head
                .Where(h => h.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                .Select(h => int.Parse(h["Content-Length:".Length..].Trim()))
                .SingleOrDefault();
            var chunked = head.Any(h => h.StartsWith("Transfer-Encoding:", StringComparison.OrdinalIgnoreCase)
                && h.Contains("chunked", StringComparison.OrdinalIgnoreCase));
            while (chunked ? !EndsWithLastChunk(received, headerLength) : received.Count < headerLength + contentLength)
            {
                var read = await stream.ReadAsync(buffer);
                if (read == 0)
                    return null;
                received.AddRange(buffer.Take(read));
            }

            var requestLine = head[0].Split(' ');
            return $"{requestLine[0]} {requestLine[1]}";
        }

        private static int HeaderLength(List<byte> received)
        {
            for (var i = 3; i < received.Count; i++)
            {
                if (received[i - 3] == '\r' && received[i - 2] == '\n' && received[i - 1] == '\r' && received[i] == '\n')
                    return i + 1;
            }
            return -1;
        }

        private static bool EndsWithLastChunk(List<byte> received, int headerLength)
        {
            var last = "0\r\n\r\n"u8.ToArray();
            return received.Count - headerLength >= last.Length
                && received.Skip(received.Count - last.Length).SequenceEqual(last);
        }

        private static async Task RespondAsync(NetworkStream stream, string json)
        {
            var body = System.Text.Encoding.UTF8.GetBytes(json);
            var head = System.Text.Encoding.ASCII.GetBytes(
                $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\nConnection: keep-alive\r\n\r\n");
            await stream.WriteAsync(head.Concat(body).ToArray());
        }
    }

    #endregion

    #region Call channel

    [Theory]
    [InlineData("whatsapp")]
    [InlineData("phone")]
    [InlineData("browser")]
    [InlineData("carrier_pigeon")]
    public async Task CallsGetAsync_ReadsChannelIncludingUnknownValues(string channel)
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""id"": ""6f1c2d3e-4a5b-4c6d-8e9f-0a1b2c3d4e5f"",
            ""object"": ""call"",
            ""kind"": ""pstn"",
            ""channel"": """ + channel + @""",
            ""direction"": ""inbound"",
            ""status"": ""completed"",
            ""handledBy"": ""dashboard"",
            ""agentId"": null,
            ""from"": ""+15555550177"",
            ""to"": ""+15555550123"",
            ""callerName"": null,
            ""calleeName"": null,
            ""startedAt"": ""2026-10-01T09:00:00.000Z"",
            ""answeredAt"": ""2026-10-01T09:00:06.000Z"",
            ""endedAt"": ""2026-10-01T09:01:30.000Z"",
            ""durationSecs"": 84,
            ""creditsCharged"": 4,
            ""billing"": ""settled"",
            ""hangupClass"": ""caller_hung_up"",
            ""recordingStatus"": null,
            ""metadata"": {}
        }");

        var call = await _client.Calls.GetAsync("6f1c2d3e-4a5b-4c6d-8e9f-0a1b2c3d4e5f");

        Assert.Equal(channel, call.Channel);
    }

    [Fact]
    public void CallChannel_ConstantsMatchTheApiValues()
    {
        Assert.Equal("phone", CallChannel.Phone);
        Assert.Equal("whatsapp", CallChannel.WhatsApp);
        Assert.Equal("browser", CallChannel.Browser);
    }

    [Fact]
    public void CallWebhook_KeepsChannelOnTheRawObject()
    {
        const string secret = "test_webhook_secret_12345";
        var payload = @"{
            ""id"": ""evt_call_1"",
            ""type"": ""call.completed"",
            ""data"": { ""object"": {
                ""id"": ""6f1c2d3e-4a5b-4c6d-8e9f-0a1b2c3d4e5f"",
                ""object"": ""call"",
                ""kind"": ""pstn"",
                ""channel"": ""whatsapp"",
                ""direction"": ""inbound"",
                ""status"": ""completed""
            } },
            ""created"": 1790000000,
            ""livemode"": true
        }";

        var evt = Webhooks.ParseEvent(payload, Webhooks.GenerateSignature(payload, secret), secret);

        Assert.Equal(Webhook.EventTypes.CallCompleted, evt.Type);
        Assert.Equal("whatsapp", evt.RawObject!.Value.GetProperty("channel").GetString());
        Assert.Equal(CallChannel.WhatsApp, evt.ObjectAs<Call>()!.Channel);
    }

    #endregion

    #region Send unconfirmed

    [Fact]
    public async Task SendAsync_WhatsAppNotSent_IsStillRetriedUnderTheSameKey()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.BadGateway,
            @"{""error"": ""whatsapp_send_failed"", ""errorCode"": ""E028"", ""message"": ""The message couldn't be delivered.""}"));
        _mockHandler.QueueSuccessResponse(@"{
            ""id"": ""msg_123"",
            ""channel"": ""whatsapp"",
            ""message_format"": ""whatsapp"",
            ""to"": ""+15555550177"",
            ""from"": ""+15555550123"",
            ""text"": ""Your table is ready!"",
            ""status"": ""queued"",
            ""segments"": 1,
            ""creditsUsed"": 1,
            ""whatsapp"": { ""kind"": ""text"", ""messageId"": null },
            ""createdAt"": ""2026-10-01T10:00:00Z"",
            ""metadata"": {}
        }");

        var message = await _client.Messages.SendAsync(new SendWhatsAppMessageRequest(
            "+15555550177",
            Sender,
            text: "Your table is ready!"
        ));

        Assert.Equal("msg_123", message.Id);
        Assert.Equal(2, _mockHandler.Requests.Count);
        Assert.Equal(
            _mockHandler.Requests[0].Headers.GetValues("Idempotency-Key").Single(),
            _mockHandler.Requests[1].Headers.GetValues("Idempotency-Key").Single());
    }

    [Fact]
    public async Task SendAsync_WhatsAppUnconfirmed_ThrowsConflictWithoutRetrying()
    {
        _mockHandler.QueueResponse(ErrorResponse(HttpStatusCode.Conflict,
            @"{""error"": ""whatsapp_send_unconfirmed"", ""errorCode"": ""E024"", ""message"": ""We couldn't confirm whether WhatsApp accepted this message. It has been marked failed and refunded, but it may still be delivered. Check before sending it again, or it could arrive twice.""}"));

        var ex = await Assert.ThrowsAsync<SendlyException>(
            () => _client.Messages.SendAsync(new SendWhatsAppMessageRequest(
                "+15555550177",
                Sender,
                text: "Your table is ready!"
            )));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("whatsapp_send_unconfirmed", ex.ApiErrorCode);
        Assert.Single(_mockHandler.Requests);
    }

    #endregion
}
