using System.Net;
using System.Reflection;
using Sendly.Exceptions;
using Sendly.Models;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for the WhatsApp to SMS fallback: the fallbackToSms request field,
/// the fallback object on the response and on a read message, and the
/// fallback refusals.
/// </summary>
public class WhatsAppSmsFallbackTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public WhatsAppSmsFallbackTests()
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

    private Task<WhatsAppMessage> SendText(bool? fallbackToSms = null)
    {
        return _client.Messages.SendAsync(new SendWhatsAppMessageRequest(
            "+15551234567",
            "+15559876543",
            text: "Your table is ready!",
            fallbackToSms: fallbackToSms
        ));
    }

    [Fact]
    public async Task SendAsync_FallbackToSmsTrue_SendsTrue()
    {
        _mockHandler.QueueSuccessResponse(@"{""id"": ""msg_123"", ""whatsapp"": {""kind"": ""text""}}");

        await SendText(true);

        var body = await _mockHandler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"channel\":\"whatsapp\"", body);
        Assert.Contains("\"fallbackToSms\":true", body);
    }

    [Fact]
    public async Task SendAsync_FallbackToSmsFalse_SendsFalse()
    {
        _mockHandler.QueueSuccessResponse(@"{""id"": ""msg_123"", ""whatsapp"": {""kind"": ""text""}}");

        await SendText(false);

        var body = await _mockHandler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"fallbackToSms\":false", body);
    }

    [Fact]
    public async Task SendAsync_FallbackToSmsUnset_OmitsField()
    {
        _mockHandler.QueueSuccessResponse(@"{""id"": ""msg_123"", ""whatsapp"": {""kind"": ""text""}}");

        await SendText();

        var body = await _mockHandler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.DoesNotContain("fallbackToSms", body);
    }

    [Fact]
    public async Task SendAsync_FallbackToSmsSetByProperty_SendsIt()
    {
        _mockHandler.QueueSuccessResponse(@"{""id"": ""msg_123"", ""whatsapp"": {""kind"": ""template""}}");

        await _client.Messages.SendAsync(new SendWhatsAppMessageRequest(
            "+15551234567",
            "+15559876543",
            template: new WhatsAppTemplateSendParams { Name = "order_shipped", Language = "en_US" })
        {
            FallbackToSms = true
        });

        var body = await _mockHandler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"fallbackToSms\":true", body);
    }

    [Fact]
    public async Task SendAsync_FellBackToSms_ParsesFallback()
    {
        var responseJson = @"{
            ""id"": ""msg_wa_1"",
            ""channel"": ""whatsapp"",
            ""message_format"": ""whatsapp"",
            ""messageFormat"": ""whatsapp"",
            ""to"": ""+15551234567"",
            ""from"": ""+15559876543"",
            ""text"": null,
            ""status"": ""failed"",
            ""direction"": ""outbound"",
            ""segments"": 1,
            ""creditsUsed"": 0,
            ""whatsapp"": {
                ""kind"": ""template"",
                ""template"": { ""name"": ""order_shipped"", ""language"": ""en_US"", ""category"": ""utility"" },
                ""messageId"": null
            },
            ""createdAt"": ""2026-10-17T10:00:00Z"",
            ""fallback"": {
                ""channel"": ""sms"",
                ""status"": ""sent"",
                ""reason"": ""window_closed"",
                ""messageId"": ""msg_sms_1"",
                ""creditsUsed"": 2,
                ""contentDropped"": [""quick_replies""]
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        var message = await SendText(true);

        Assert.Equal("failed", message.Status);
        Assert.NotNull(message.Fallback);
        Assert.Equal("sms", message.Fallback!.Channel);
        Assert.Equal(WhatsAppSmsFallback.Statuses.Sent, message.Fallback.Status);
        Assert.Equal(WhatsAppSmsFallback.Reasons.WindowClosed, message.Fallback.Reason);
        Assert.Equal("msg_sms_1", message.Fallback.MessageId);
        Assert.Equal(2, message.Fallback.CreditsUsed);
        Assert.Null(message.Fallback.Error);
        Assert.Equal(new List<string> { "quick_replies" }, message.Fallback.ContentDropped);
        Assert.True(message.Fallback.IsSent);
        Assert.False(message.Fallback.IsPending);
        Assert.True(message.FellBack);
    }

    [Fact]
    public async Task SendAsync_FallbackPending_IsNotFellBack()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""id"": ""msg_wa_2"",
            ""channel"": ""whatsapp"",
            ""status"": ""sent"",
            ""whatsapp"": {""kind"": ""text"", ""messageId"": ""wamid.1""},
            ""fallback"": {""channel"": ""sms"", ""status"": ""pending""}
        }");

        var message = await SendText(true);

        Assert.NotNull(message.Fallback);
        Assert.True(message.Fallback!.IsPending);
        Assert.Null(message.Fallback.Reason);
        Assert.Null(message.Fallback.MessageId);
        Assert.Null(message.Fallback.CreditsUsed);
        Assert.Null(message.Fallback.ContentDropped);
        Assert.False(message.FellBack);
    }

    [Fact]
    public async Task SendAsync_FallbackWithUnknownValues_KeepsThem()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""id"": ""msg_wa_3"",
            ""whatsapp"": {""kind"": ""text""},
            ""fallback"": {""channel"": ""sms"", ""status"": ""rerouted"", ""reason"": ""something_new""}
        }");

        var message = await SendText(true);

        Assert.Equal("rerouted", message.Fallback!.Status);
        Assert.Equal("something_new", message.Fallback.Reason);
        Assert.False(message.FellBack);
    }

    [Fact]
    public async Task SendAsync_ResponseWithoutFallback_LeavesFallbackNull()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""id"": ""msg_123"",
            ""channel"": ""whatsapp"",
            ""status"": ""sent"",
            ""whatsapp"": {""kind"": ""text"", ""messageId"": null}
        }");

        var message = await SendText();

        Assert.Null(message.Fallback);
        Assert.False(message.FellBack);
    }

    [Fact]
    public async Task GetAsync_WhatsAppMessage_ParsesFallback()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""id"": ""msg_wa_1"",
            ""to"": ""+15551234567"",
            ""from"": ""+15559876543"",
            ""text"": ""Your table is ready!"",
            ""status"": ""failed"",
            ""direction"": ""outbound"",
            ""error"": null,
            ""errorCode"": null,
            ""retryCount"": 0,
            ""segments"": 1,
            ""creditsUsed"": 0,
            ""isSandbox"": false,
            ""createdAt"": ""2026-10-17T10:00:00Z"",
            ""deliveredAt"": null,
            ""message_format"": ""whatsapp"",
            ""messageFormat"": ""whatsapp"",
            ""fallback"": {
                ""channel"": ""sms"",
                ""status"": ""failed"",
                ""reason"": ""whatsapp_failed"",
                ""messageId"": ""msg_sms_1"",
                ""error"": ""Not enough credits for the SMS.""
            }
        }");

        var message = await _client.Messages.GetAsync("msg_wa_1");

        Assert.NotNull(message.Fallback);
        Assert.Equal(WhatsAppSmsFallback.Statuses.Failed, message.Fallback!.Status);
        Assert.Equal(WhatsAppSmsFallback.Reasons.WhatsAppFailed, message.Fallback.Reason);
        Assert.Equal("msg_sms_1", message.Fallback.MessageId);
        Assert.Equal("Not enough credits for the SMS.", message.Fallback.Error);
        Assert.Null(message.FallbackOf);
    }

    [Fact]
    public async Task GetAsync_SmsFallback_ParsesFallbackOf()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""id"": ""msg_sms_1"",
            ""to"": ""+15551234567"",
            ""from"": ""+15559876543"",
            ""text"": ""Your table is ready!"",
            ""status"": ""delivered"",
            ""direction"": ""outbound"",
            ""segments"": 1,
            ""creditsUsed"": 2,
            ""createdAt"": ""2026-10-17T10:00:01Z"",
            ""message_format"": ""sms"",
            ""messageFormat"": ""sms"",
            ""fallbackOf"": ""msg_wa_1""
        }");

        var message = await _client.Messages.GetAsync("msg_sms_1");

        Assert.Equal("msg_wa_1", message.FallbackOf);
        Assert.Null(message.Fallback);
    }

    [Fact]
    public async Task GetAsync_MessageWithoutFallback_LeavesBothNull()
    {
        _mockHandler.QueueSuccessResponse(@"{""id"": ""msg_1"", ""to"": ""+15551234567"", ""text"": ""Hi"", ""status"": ""delivered""}");

        var message = await _client.Messages.GetAsync("msg_1");

        Assert.Null(message.Fallback);
        Assert.Null(message.FallbackOf);
    }

    [Fact]
    public async Task SendAsync_SmsFallbackUnavailable_CarriesCode()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Forbidden, @"{
            ""error"": ""sms_fallback_unavailable"",
            ""message"": ""+15559876543 isn't set up to send SMS yet."",
            ""suggestion"": ""Send without fallbackToSms, or make sure this number can send SMS to this recipient first.""
        }");

        var exception = await Assert.ThrowsAsync<SendlyException>(() => SendText(true));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal("sms_fallback_unavailable", exception.ApiErrorCode);
        Assert.Equal("+15559876543 isn't set up to send SMS yet.", exception.Message);
        Assert.Equal(
            "Send without fallbackToSms, or make sure this number can send SMS to this recipient first.",
            exception.ResponseBody!.Value.GetProperty("suggestion").GetString());
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task SendAsync_SmsFallbackNotSupported_CarriesCode()
    {
        _mockHandler.QueueResponse(HttpStatusCode.UnprocessableEntity, @"{
            ""error"": ""sms_fallback_not_supported"",
            ""reason"": ""marketing_template"",
            ""message"": ""Marketing templates can't fall back to SMS: SMS marketing needs its own consent. Send without fallbackToSms.""
        }");

        var exception = await Assert.ThrowsAsync<ValidationException>(() => SendText(true));

        Assert.Equal(422, exception.StatusCode);
        Assert.Equal("sms_fallback_not_supported", exception.ApiErrorCode);
        Assert.Equal("marketing_template", exception.ResponseBody!.Value.GetProperty("reason").GetString());
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task SendAsync_WindowClosedWithFailedFallback_CarriesFallbackInBody()
    {
        _mockHandler.QueueResponse(HttpStatusCode.UnprocessableEntity, @"{
            ""error"": ""whatsapp_window_closed"",
            ""message"": ""The 24-hour window is closed."",
            ""suggestion"": ""Send a template."",
            ""id"": ""msg_wa_4"",
            ""fallback"": {""channel"": ""sms"", ""status"": ""unavailable"", ""reason"": ""window_closed"", ""error"": ""This contact has opted out of messages.""}
        }");

        var exception = await Assert.ThrowsAsync<ValidationException>(() => SendText(true));

        Assert.Equal("whatsapp_window_closed", exception.ApiErrorCode);
        var body = exception.ResponseBody!.Value;
        Assert.Equal("msg_wa_4", body.GetProperty("id").GetString());
        Assert.Equal("unavailable", body.GetProperty("fallback").GetProperty("status").GetString());
    }
}
