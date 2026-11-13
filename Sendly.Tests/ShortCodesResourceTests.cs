using System.Net;
using System.Reflection;
using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;
using Sendly.Resources;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for ShortCodesResource against the bodies the short code endpoints send.
/// </summary>
public class ShortCodesResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public ShortCodesResourceTests()
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

    private async Task<JsonElement> JsonBodyOfLastRequest()
    {
        using var doc = JsonDocument.Parse(await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    private const string LeaseJson = @"{
        ""monthlyCents"": 115000,
        ""minimumTermMonths"": 3,
        ""startsAt"": ""go_live"",
        ""state"": ""past_due"",
        ""billingEnabled"": true,
        ""startedAt"": ""2026-10-05T08:00:00.000Z"",
        ""nextChargeAt"": ""2026-11-05T08:00:00.000Z"",
        ""paidThrough"": null,
        ""termEndsAt"": ""2027-01-05T08:00:00.000Z"",
        ""endsAt"": null,
        ""cancelRequestedAt"": null,
        ""pastDue"": {
            ""chargeId"": ""charge_1"",
            ""amountCents"": 115000,
            ""status"": ""failed"",
            ""periodStart"": ""2026-10-05T08:00:00.000Z"",
            ""periodEnd"": ""2026-11-05T08:00:00.000Z"",
            ""firstFailedAt"": ""2026-10-05T08:00:00.000Z"",
            ""pauseAt"": ""2026-10-12T08:00:00.000Z"",
            ""noticeStage"": ""failed""
        },
        ""dueMonths"": 0,
        ""dueCents"": 0
    }";

    private static readonly string ViewJson = @"{
        ""enabled"": true,
        ""application"": {
            ""id"": ""sc_1"",
            ""shortCode"": null,
            ""countryCode"": ""US"",
            ""status"": ""requested"",
            ""reviewStatus"": ""approved_for_filing"",
            ""reviewNote"": null,
            ""orderType"": ""new"",
            ""codeType"": ""random"",
            ""contentProviderSameAsBrand"": true,
            ""sampleMessages"": [""Acme: your order shipped. Reply STOP to opt out.""],
            ""useCase"": ""Order and delivery alerts"",
            ""optInFlow"": ""Customers text JOIN to the code after checkout."",
            ""expectedMonthlyVolume"": ""50000"",
            ""expectedDailyVolume"": ""2000"",
            ""helpResponse"": null,
            ""stopConfirmation"": null,
            ""optInConfirmation"": null,
            ""privacyPolicyUrl"": ""https://acme.example/privacy"",
            ""termsUrl"": ""https://acme.example/terms"",
            ""messageFrequency"": ""Up to 4 messages per order"",
            ""campaignKeyword"": ""JOIN"",
            ""requestedDigits"": null,
            ""losingProvider"": null,
            ""brandContactName"": ""Ada Lovelace"",
            ""brandContactEmail"": ""ada@acme.example"",
            ""brandContactPhone"": ""+15125550100"",
            ""contentProviderLegalName"": null,
            ""contentProviderEin"": null,
            ""contentProviderContactName"": null,
            ""contentProviderContactEmail"": null,
            ""contentProviderContactPhone"": null,
            ""brandRegistrationStatus"": ""registered"",
            ""contentProviderRegistrationStatus"": ""not_started"",
            ""submittedAt"": ""2026-09-01T10:00:00.000Z"",
            ""reviewedAt"": ""2026-09-03T10:00:00.000Z"",
            ""filedAt"": null,
            ""activatedAt"": null,
            ""registryRevettingDueAt"": null,
            ""createdAt"": ""2026-08-30T10:00:00.000Z"",
            ""updatedAt"": ""2026-09-03T10:00:00.000Z""
        },
        ""prefill"": { ""source"": ""tendlc"", ""legalName"": ""Acme Inc"", ""contact"": { ""email"": ""ada@acme.example"" } },
        ""editable"": false,
        ""lockMessage"": ""Sendly approved this application and is filing it with the carriers."",
        ""canStartNewApplication"": false,
        ""generatedDefaults"": null,
        ""requiredDocuments"": [""loa""],
        ""missingDocuments"": [],
        ""documents"": [
            { ""kind"": ""loa"", ""templateReady"": true, ""signedAt"": ""2026-09-02T10:00:00.000Z"", ""signedByName"": ""Ada Lovelace"", ""awaitingSignature"": false }
        ],
        ""quote"": {
            ""codeType"": ""random"",
            ""monthlyUsd"": 1150,
            ""setupUsd"": 999,
            ""currency"": ""USD"",
            ""autoBilled"": false,
            ""setupCents"": 99900,
            ""monthlyCents"": 115000,
            ""minimumTermMonths"": 3,
            ""setupChargedAt"": ""submit"",
            ""leaseStartsAt"": ""go_live""
        },
        ""billing"": {
            ""setupFee"": { ""amountCents"": 99900, ""status"": ""paid"", ""chargedAt"": ""submit"", ""paidAt"": ""2026-09-01T10:00:00.000Z"", ""refundedCents"": 0, ""refundedAt"": null },
            ""lease"": " + LeaseJson + @",
            ""terms"": { ""version"": ""2026-10-06"", ""acceptedVersion"": ""2026-10-06"", ""acceptedAt"": ""2026-09-01T10:00:00.000Z"" },
            ""refundPolicy"": ""full_refund_before_filing"",
            ""exempt"": null,
            ""charges"": [
                { ""id"": ""charge_0"", ""kind"": ""setup"", ""amountCents"": 99900, ""status"": ""paid"", ""paidAt"": ""2026-09-01T10:00:00.000Z"", ""refundedCents"": 0, ""refundedAt"": null, ""periodStart"": null, ""periodEnd"": null, ""createdAt"": ""2026-09-01T10:00:00.000Z"" }
            ]
        },
        ""verification"": { ""verified"": true, ""state"": ""verified"", ""href"": ""/verify"" },
        ""carriers"": {
            ""approved"": 1,
            ""total"": 4,
            ""overall"": ""in_progress"",
            ""byCarrier"": [
                { ""carrier"": ""ATT"", ""label"": ""AT&T"", ""status"": ""approved"", ""updatedAt"": ""2026-09-10T10:00:00.000Z"" },
                { ""carrier"": ""TMOBILE"", ""label"": ""T-Mobile"", ""status"": ""in_review"", ""updatedAt"": null },
                { ""carrier"": ""VERIZON"", ""label"": ""Verizon"", ""status"": ""submitted"", ""updatedAt"": null },
                { ""carrier"": ""US_CELLULAR"", ""label"": ""US Cellular"", ""status"": ""not_submitted"", ""updatedAt"": null }
            ]
        }
    }";

    [Fact]
    public async Task ListAsync_ReadsEachCodeWithItsBilling()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""shortCodes"": [{
                ""id"": ""sc_1"",
                ""shortCode"": ""55123"",
                ""countryCode"": ""US"",
                ""status"": ""active"",
                ""reviewStatus"": ""filed"",
                ""useCase"": ""Order and delivery alerts"",
                ""createdAt"": ""2026-08-30T10:00:00.000Z"",
                ""billing"": { ""setupFee"": { ""status"": ""paid"", ""amountCents"": 99900 }, ""lease"": " + LeaseJson + @" }
            }]
        }");

        var result = await _client.ShortCodes.ListAsync();

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.EndsWith("/short_codes", request.RequestUri!.AbsolutePath);
        var code = Assert.Single(result.ShortCodes);
        Assert.Equal("55123", code.Code);
        Assert.Equal(ShortCodeStatus.Active, code.Status);
        Assert.Equal(ShortCodeReviewStatus.Filed, code.ReviewStatus);
        Assert.Equal("2026-08-30T10:00:00.000Z", code.CreatedAt);
        Assert.Equal("paid", code.Billing!.SetupFee.Status);
        Assert.Equal(99900, code.Billing.SetupFee.AmountCents);
        Assert.Equal("past_due", code.Billing.Lease.State);
        Assert.Equal("2026-10-12T08:00:00.000Z", code.Billing.Lease.PastDue!.PauseAt);
    }

    [Fact]
    public async Task ApplicationGetAsync_ReadsTheApplicationQuoteBillingAndCarriers()
    {
        _mockHandler.QueueSuccessResponse(ViewJson);

        var view = await _client.ShortCodes.Application.GetAsync();

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.EndsWith("/short_codes/application", request.RequestUri!.AbsolutePath);
        Assert.True(view.Enabled);
        Assert.False(view.Editable);
        Assert.Equal("sc_1", view.Application.Id);
        Assert.Null(view.Application.Code);
        Assert.Equal(ShortCodeReviewStatus.ApprovedForFiling, view.Application.ReviewStatus);
        Assert.Equal("2026-09-03T10:00:00.000Z", view.Application.ReviewedAt);
        Assert.Null(view.Application.RegistryRevettingDueAt);
        Assert.Single(view.Application.SampleMessages);
        Assert.Equal("tendlc", view.Prefill.Source);
        Assert.Equal("ada@acme.example", view.Prefill.Contact!.Email);
        Assert.Equal("loa", Assert.Single(view.RequiredDocuments));
        Assert.Equal("Ada Lovelace", Assert.Single(view.Documents).SignedByName);
        Assert.Equal(999, view.Quote.SetupUsd);
        Assert.Equal(115000, view.Quote.MonthlyCents);
        Assert.Equal(3, view.Quote.MinimumTermMonths);
        Assert.Equal("paid", view.Billing!.SetupFee.Status);
        Assert.Equal("submit", view.Billing.SetupFee.ChargedAt);
        Assert.Equal("go_live", view.Billing.Lease.StartsAt);
        Assert.Equal("2026-10-06", view.Billing.Terms.AcceptedVersion);
        Assert.Null(view.Billing.Exempt);
        Assert.Equal("setup", Assert.Single(view.Billing.Charges).Kind);
        Assert.Equal("verified", view.Verification.State);
        Assert.Equal(4, view.Carriers.Total);
        Assert.Equal("in_review", view.Carriers.ByCarrier[1].Status);
        Assert.Null(view.Carriers.ByCarrier[1].UpdatedAt);
        Assert.Null(view.Submitted);
        Assert.Null(view.IgnoredFields);
    }

    [Fact]
    public async Task ApplicationUpdateAsync_PutsOnlyTheAnswersSetWithTheIdempotencyKey()
    {
        _mockHandler.QueueSuccessResponse(ViewJson.Replace(@"""enabled"": true,", @"""enabled"": true, ""ignoredFields"": [""status""],"));

        var view = await _client.ShortCodes.Application.UpdateAsync(
            new ShortCodeApplicationInput
            {
                UseCase = "Delivery alerts for Acme orders",
                MessageFrequency = "4 messages per month",
                ContentProviderSameAsBrand = true,
                SampleMessages = new List<string> { "Acme: your order shipped." },
            },
            new IdempotentRequestOptions { IdempotencyKey = "sc-save-1" });

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.EndsWith("/short_codes/application", request.RequestUri!.AbsolutePath);
        Assert.Equal("sc-save-1", request.Headers.GetValues("Idempotency-Key").Single());
        var body = await JsonBodyOfLastRequest();
        Assert.Equal("Delivery alerts for Acme orders", body.GetProperty("useCase").GetString());
        Assert.Equal("4 messages per month", body.GetProperty("messageFrequency").GetString());
        Assert.True(body.GetProperty("contentProviderSameAsBrand").GetBoolean());
        Assert.Equal(1, body.GetProperty("sampleMessages").GetArrayLength());
        Assert.Equal(4, body.EnumerateObject().Count());
        Assert.Equal("status", Assert.Single(view.IgnoredFields!));
    }

    [Fact]
    public async Task ApplicationUpdateAsync_WithoutOptions_SendsNoIdempotencyKey()
    {
        _mockHandler.QueueSuccessResponse(ViewJson);

        await _client.ShortCodes.Application.UpdateAsync(new ShortCodeApplicationInput { UseCase = "Alerts" });

        Assert.False(_mockHandler.LastRequest!.Headers.Contains("Idempotency-Key"));
    }

    [Fact]
    public async Task ApplicationCheckAsync_PostsToPreflightAndReadsIssues()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""ok"": false,
            ""issues"": [{ ""path"": ""helpResponse"", ""message"": ""Add a HELP reply."" }],
            ""requiredDocuments"": [""loa""],
            ""missingDocuments"": [""loa""],
            ""documents"": []
        }");

        var check = await _client.ShortCodes.Application.CheckAsync();

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/short_codes/application/preflight", request.RequestUri!.AbsolutePath);
        Assert.Equal("{}", await request.Content!.ReadAsStringAsync());
        Assert.False(check.Ok);
        var issue = Assert.Single(check.Issues);
        Assert.Equal("helpResponse", issue.Path);
        Assert.Equal("Add a HELP reply.", issue.Message);
        Assert.Equal("loa", Assert.Single(check.MissingDocuments));
        Assert.Empty(check.Documents);
    }

    [Fact]
    public async Task ApplicationCheckAsync_WithUnsavedAnswers_SendsThem()
    {
        _mockHandler.QueueSuccessResponse(@"{ ""ok"": true, ""issues"": [], ""requiredDocuments"": [], ""missingDocuments"": [], ""documents"": [] }");

        var check = await _client.ShortCodes.Application.CheckAsync(new ShortCodeApplicationInput { HelpResponse = "Acme alerts: help@acme.example" });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("Acme alerts: help@acme.example", body.GetProperty("helpResponse").GetString());
        Assert.True(check.Ok);
    }

    [Fact]
    public async Task ApplicationSubmitAsync_SendsAcceptTermsAndTheKeyAndReadsThePayment()
    {
        _mockHandler.QueueSuccessResponse(ViewJson.Replace(
            @"""enabled"": true,",
            @"""enabled"": true, ""submitted"": true, ""alreadySubmitted"": false, ""notified"": true, ""payment"": { ""status"": ""paid"", ""charged"": true },"));

        var view = await _client.ShortCodes.Application.SubmitAsync(
            new SubmitShortCodeApplicationRequest { AcceptTerms = true, CampaignKeyword = "JOIN" },
            new IdempotentRequestOptions { IdempotencyKey = "sc-submit-1" });

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/short_codes/application/submit", request.RequestUri!.AbsolutePath);
        Assert.Equal("sc-submit-1", request.Headers.GetValues("Idempotency-Key").Single());
        var body = await JsonBodyOfLastRequest();
        Assert.True(body.GetProperty("acceptTerms").GetBoolean());
        Assert.Equal("JOIN", body.GetProperty("campaignKeyword").GetString());
        Assert.Equal(2, body.EnumerateObject().Count());
        Assert.True(view.Submitted);
        Assert.False(view.AlreadySubmitted);
        Assert.True(view.Notified);
        Assert.Equal("paid", view.Payment!.Status);
        Assert.True(view.Payment.Charged);
    }

    [Fact]
    public async Task ApplicationSubmitAsync_WhenTheAnswersAreIncomplete_ThrowsValidationException()
    {
        _mockHandler.QueueResponse(HttpStatusCode.UnprocessableEntity, @"{
            ""error"": ""short_code_invalid_application"",
            ""message"": ""The application is not complete."",
            ""errors"": [{ ""path"": ""acceptTerms"", ""message"": ""Accept the terms."" }]
        }");

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _client.ShortCodes.Application.SubmitAsync(new SubmitShortCodeApplicationRequest()));

        Assert.Equal(422, ex.StatusCode);
    }

    [Fact]
    public async Task ApplicationGetAsync_WhenShortCodesAreNotEnabled_ThrowsNotFoundException()
    {
        _mockHandler.QueueResponse(HttpStatusCode.NotFound, @"{ ""error"": ""short_codes_not_enabled"", ""message"": ""Short codes are not enabled."" }");

        await Assert.ThrowsAsync<NotFoundException>(() => _client.ShortCodes.Application.GetAsync());
    }
}
