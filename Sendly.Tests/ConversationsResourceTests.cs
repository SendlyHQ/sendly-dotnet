using System.Net;
using System.Reflection;
using System.Text.Json;
using Sendly.Models;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for the conversations, labels and drafts resources against the
/// camelCase rows those endpoints send.
/// </summary>
public class ConversationsResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public ConversationsResourceTests()
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

    private async Task<string> BodyOfLastRequest()
    {
        return await _mockHandler.LastRequest!.Content!.ReadAsStringAsync();
    }

    private async Task<JsonElement> JsonBodyOfLastRequest()
    {
        using var doc = JsonDocument.Parse(await BodyOfLastRequest());
        return doc.RootElement.Clone();
    }

    private const string ConversationRow = @"{
        ""id"": ""conv_1"",
        ""userId"": ""user_1"",
        ""organizationId"": ""org_1"",
        ""phoneNumber"": ""+15550001111"",
        ""status"": ""active"",
        ""unreadCount"": 2,
        ""messageCount"": 5,
        ""lastMessageText"": ""See you then"",
        ""lastMessageAt"": ""2026-09-25T10:00:00.000Z"",
        ""lastMessageDirection"": ""inbound"",
        ""channel"": ""sms"",
        ""metadata"": {},
        ""tags"": [""vip""],
        ""contactId"": ""ct_1"",
        ""createdAt"": ""2026-09-01T00:00:00.000Z"",
        ""updatedAt"": ""2026-09-25T10:00:00.000Z""
    }";

    private const string DraftRow = @"{
        ""id"": ""d_1"",
        ""userId"": ""user_1"",
        ""organizationId"": ""org_1"",
        ""conversationId"": ""conv_1"",
        ""text"": ""Thanks, see you Friday"",
        ""mediaUrls"": [""https://cdn.example/a.jpg""],
        ""metadata"": {},
        ""status"": ""rejected"",
        ""source"": ""ai"",
        ""createdBy"": ""key_1"",
        ""reviewedBy"": ""user_1"",
        ""reviewedAt"": ""2026-09-25T10:05:00.000Z"",
        ""rejectionReason"": ""Too formal"",
        ""messageId"": null,
        ""createdAt"": ""2026-09-25T10:00:00.000Z"",
        ""updatedAt"": ""2026-09-25T10:05:00.000Z""
    }";

    #region Conversations

    [Fact]
    public async Task ListAsync_ReadsTheConversationRowsTheApiSends()
    {
        var item = ConversationRow.TrimEnd().TrimEnd('}') + @",""isGroup"": false }";
        _mockHandler.QueueSuccessResponse($@"{{
            ""data"": [{item}],
            ""pagination"": {{ ""total"": 3, ""limit"": 1, ""offset"": 0, ""hasMore"": true }}
        }}");

        var result = await _client.Conversations.ListAsync(new ListConversationsOptions { Limit = 1 });

        var conversation = Assert.Single(result.Data);
        Assert.Equal("+15550001111", conversation.PhoneNumber);
        Assert.Equal(2, conversation.UnreadCount);
        Assert.Equal(5, conversation.MessageCount);
        Assert.Equal("2026-09-25T10:00:00.000Z", conversation.LastMessageAt);
        Assert.Equal("ct_1", conversation.ContactId);
        Assert.Equal("See you then", conversation.LastMessageText);
        Assert.Equal("inbound", conversation.LastMessageDirection);
        Assert.Equal("2026-09-01T00:00:00.000Z", conversation.CreatedAt);
        Assert.Equal(3, result.Pagination.Total);
        Assert.True(result.Pagination.HasMore);
        Assert.Equal("sms", conversation.Channel);
        Assert.False(conversation.IsGroup);
        Assert.Null(conversation.Participants);
    }

    [Fact]
    public async Task ListAsync_ReadsAGroupThreadsParticipants()
    {
        var item = ConversationRow
            .Replace(@"""phoneNumber"": ""+15550001111""", @"""phoneNumber"": ""grp_abc""")
            .Replace(@"""metadata"": {}", @"""metadata"": { ""isGroup"": true, ""participants"": [""+15550001111"", ""+15550002222""] }")
            .TrimEnd().TrimEnd('}') + @",""isGroup"": true, ""participants"": [""+15550001111"", ""+15550002222""] }";
        _mockHandler.QueueSuccessResponse($@"{{
            ""data"": [{item}],
            ""pagination"": {{ ""total"": 1, ""limit"": 50, ""offset"": 0, ""hasMore"": false }}
        }}");

        var result = await _client.Conversations.ListAsync();

        var conversation = Assert.Single(result.Data);
        Assert.True(conversation.IsGroup);
        Assert.Equal(new[] { "+15550001111", "+15550002222" }, conversation.Participants);
        Assert.False(result.Pagination.HasMore);
    }

    [Fact]
    public async Task GetAsync_ReadsTheConversationAndItsMessagesPage()
    {
        var withMessages = ConversationRow.TrimEnd().TrimEnd('}') + @",
            ""isGroup"": false,
            ""messages"": {
                ""data"": [{
                    ""id"": ""msg_1"",
                    ""to"": ""+18335550100"",
                    ""from"": ""+15550001111"",
                    ""text"": ""See you then"",
                    ""direction"": ""inbound"",
                    ""status"": ""delivered"",
                    ""error"": null,
                    ""errorCode"": null,
                    ""retryCount"": 0,
                    ""isSandbox"": false,
                    ""segments"": 1,
                    ""creditsUsed"": 0,
                    ""createdAt"": ""2026-09-25T10:00:00.000Z"",
                    ""deliveredAt"": null
                }],
                ""pagination"": { ""total"": 7, ""limit"": 1, ""offset"": 0, ""hasMore"": true }
            }
        }";
        _mockHandler.QueueSuccessResponse(withMessages);

        var conversation = await _client.Conversations.GetAsync("conv_1",
            new GetConversationOptions { IncludeMessages = true, MessageLimit = 1 });

        Assert.Equal("+15550001111", conversation.PhoneNumber);
        Assert.NotNull(conversation.Messages);
        Assert.True(conversation.Messages!.Pagination.HasMore);
        Assert.Equal(7, conversation.Messages.Pagination.Total);
        var message = Assert.Single(conversation.Messages.Data);
        Assert.Equal("inbound", message.Direction);
        Assert.NotEqual(default, message.CreatedAt);
    }

    [Fact]
    public async Task CloseAsync_ReadsTheUpdatedRow()
    {
        _mockHandler.QueueSuccessResponse(ConversationRow.Replace(@"""status"": ""active""", @"""status"": ""closed"""));

        var conversation = await _client.Conversations.CloseAsync("conv_1");

        Assert.Equal("closed", conversation.Status);
        Assert.Equal("+15550001111", conversation.PhoneNumber);
        Assert.Equal(2, conversation.UnreadCount);
    }

    [Fact]
    public async Task GetContextAsync_ReadsTheContextTheApiSends()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""context"": ""[2026-09-25 10:00] Customer: See you then"",
            ""conversation"": { ""id"": ""conv_1"", ""phoneNumber"": ""+15550001111"", ""status"": ""active"", ""messageCount"": 5, ""unreadCount"": 2 },
            ""tokenEstimate"": 12,
            ""business"": { ""name"": ""Acme Dental"", ""useCase"": ""Appointment reminders"" }
        }");

        var context = await _client.Conversations.GetContextAsync("conv_1");

        Assert.Equal(12, context.TokenEstimate);
        Assert.Equal("+15550001111", context.Conversation.PhoneNumber);
        Assert.Equal(5, context.Conversation.MessageCount);
        Assert.Equal(2, context.Conversation.UnreadCount);
        Assert.Equal("Appointment reminders", context.Business!.UseCase);
    }

    [Fact]
    public async Task AddLabelsAsync_SendsTheLabelIdsKeyTheApiReads()
    {
        _mockHandler.QueueSuccessResponse(@"{""data"":[{""id"":""lbl_1"",""name"":""VIP"",""color"":""#f00"",""description"":null,""createdAt"":""2026-09-01T00:00:00.000Z""}]}");

        var labels = await _client.Conversations.AddLabelsAsync("conv_1", new AddLabelsRequest { LabelIds = new List<string> { "lbl_1" } });

        Assert.Equal("{\"labelIds\":[\"lbl_1\"]}", await BodyOfLastRequest());
        Assert.Equal("lbl_1", Assert.Single(labels.Data).Id);
    }

    [Fact]
    public async Task VerifyListAsync_StillReadsTheSnakeCaseHasMoreItsEndpointSends()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""verifications"": [],
            ""pagination"": { ""limit"": 20, ""has_more"": true }
        }");

        var list = await _client.Verify.ListAsync();

        Assert.True(list.Pagination!.HasMore);
        Assert.Equal(20, list.Pagination.Limit);
    }

    #endregion

    #region Labels

    [Fact]
    public async Task LabelsCreateAsync_ReadsTheCreatedAtTheApiSends()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created,
            @"{""id"":""lbl_1"",""userId"":""user_1"",""organizationId"":""org_1"",""name"":""VIP"",""color"":""#6b7280"",""description"":null,""createdAt"":""2026-09-01T00:00:00.000Z""}");

        var label = await _client.Labels.CreateAsync(new CreateLabelRequest { Name = "VIP" });

        Assert.Equal("VIP", label.Name);
        Assert.Equal("#6b7280", label.Color);
        Assert.Equal("2026-09-01T00:00:00.000Z", label.CreatedAt);
        Assert.Equal("{\"name\":\"VIP\"}", await BodyOfLastRequest());
    }

    #endregion

    #region Drafts

    [Fact]
    public async Task DraftsCreateAsync_SendsTheKeysTheApiReads()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, DraftRow);

        await _client.Drafts.CreateAsync(new CreateDraftRequest
        {
            ConversationId = "conv_1",
            Text = "hi",
            MediaUrls = new List<string> { "https://x/a.jpg" }
        });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("conv_1", body.GetProperty("conversationId").GetString());
        Assert.Equal("https://x/a.jpg", body.GetProperty("mediaUrls")[0].GetString());
        Assert.False(body.TryGetProperty("conversation_id", out _));
        Assert.False(body.TryGetProperty("media_urls", out _));
    }

    [Fact]
    public async Task DraftsUpdateAsync_SendsTheMediaUrlsKeyTheApiReads()
    {
        _mockHandler.QueueSuccessResponse(DraftRow);

        await _client.Drafts.UpdateAsync("d_1", new UpdateDraftRequest
        {
            MediaUrls = new List<string> { "https://x/b.jpg" }
        });

        Assert.Equal("{\"mediaUrls\":[\"https://x/b.jpg\"]}", await BodyOfLastRequest());
    }

    [Fact]
    public async Task DraftsRejectAsync_ReadsTheDraftRowTheApiSends()
    {
        _mockHandler.QueueSuccessResponse(DraftRow);

        var draft = await _client.Drafts.RejectAsync("d_1", "Too formal");

        Assert.Equal("Too formal", draft.RejectionReason);
        Assert.Equal("conv_1", draft.ConversationId);
        Assert.Equal("2026-09-25T10:05:00.000Z", draft.ReviewedAt);
        Assert.Equal("user_1", draft.ReviewedBy);
        Assert.Equal("key_1", draft.CreatedBy);
        Assert.Equal("2026-09-25T10:00:00.000Z", draft.CreatedAt);
        Assert.Equal("https://cdn.example/a.jpg", Assert.Single(draft.MediaUrls!));
    }

    [Fact]
    public async Task DraftsApproveAsync_ReadsTheSentMessageId()
    {
        _mockHandler.QueueSuccessResponse(DraftRow
            .Replace(@"""status"": ""rejected""", @"""status"": ""approved""")
            .Replace(@"""messageId"": null", @"""messageId"": ""msg_9"""));

        var draft = await _client.Drafts.ApproveAsync("d_1");

        Assert.Equal("approved", draft.Status);
        Assert.Equal("msg_9", draft.MessageId);
    }

    [Fact]
    public async Task DraftsListAsync_ReadsTheDraftRows()
    {
        _mockHandler.QueueSuccessResponse($@"{{ ""data"": [{DraftRow}], ""pagination"": {{ ""total"": 1 }} }}");

        var list = await _client.Drafts.ListAsync(new ListDraftsOptions { ConversationId = "conv_1" });

        var draft = Assert.Single(list.Data);
        Assert.Equal("conv_1", draft.ConversationId);
        Assert.Equal(1, list.Pagination.Total);
        Assert.Contains("conversation_id=conv_1", _mockHandler.LastRequest!.RequestUri!.Query);
    }

    #endregion
}
