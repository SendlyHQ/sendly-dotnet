<p align="center">
  <img src="https://raw.githubusercontent.com/SendlyHQ/sendly-dotnet/main/.github/header.svg" alt="Sendly .NET SDK" />
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/Sendly"><img src="https://img.shields.io/nuget/v/Sendly.svg?style=flat-square" alt="NuGet" /></a>
  <a href="https://github.com/SendlyHQ/sendly-dotnet/blob/main/LICENSE"><img src="https://img.shields.io/github/license/SendlyHQ/sendly-dotnet?style=flat-square" alt="license" /></a>
</p>

# Sendly .NET SDK

Official .NET SDK for the Sendly messaging API.

## Requirements

- .NET 8.0+

## Installation

```bash
# .NET CLI
dotnet add package Sendly

# Package Manager Console
Install-Package Sendly

# PackageReference (add to .csproj)
<PackageReference Include="Sendly" Version="4.3.0" />
```

## Namespaces

Types live in four namespaces. Add the ones a snippet needs:

```csharp
using Sendly;            // SendlyClient, SendlyClientOptions, Webhooks, WebhookEvent
using Sendly.Models;     // Message, SendMessageRequest, Webhook, Account, Credits, ...
using Sendly.Resources;  // Call, VoiceMode, RcsBrandInput, ShortLink, TenDlcBrand, ...
using Sendly.Exceptions; // SendlyException and its subclasses
```

## Quick Start

```csharp
using Sendly;

using var client = new SendlyClient("sk_live_v1_your_api_key");

// Send an SMS
var message = await client.Messages.SendAsync(
    "+12025550143",
    "Hello from Sendly!"
);

Console.WriteLine(message.Id);     // "4a7c1e2f-9b3d-4c8a-91f2-7d5e6a0b3c19"
Console.WriteLine(message.Status); // "queued"
```

## Prerequisites for Live Messaging

Before sending live SMS messages, you need:

1. **Business Verification** - Complete verification in the [Sendly dashboard](https://sendly.live/dashboard)
   - **International**: Instant approval (just provide Sender ID)
   - **US/Canada**: Requires carrier approval

2. **Credits** - Add credits to your account
   - Test keys (`sk_test_*`) work without credits (sandbox mode)
   - Live keys (`sk_live_*`) require credits for each message

3. **Live API Key** - Generate after verification + credits
   - Dashboard → API Keys → Create Live Key

### Test vs Live Keys

| Key Type | Prefix | Credits Required | Verification Required | Use Case |
|----------|--------|------------------|----------------------|----------|
| Test | `sk_test_v1_*` | No | No | Development, testing |
| Live | `sk_live_v1_*` | Yes | Yes | Production messaging |

> **Note**: You can start development immediately with a test key. Messages to sandbox test numbers are free and don't require verification.

## Configuration

```csharp
using var client = new SendlyClient("sk_live_v1_xxx", new SendlyClientOptions
{
    BaseUrl = "https://sendly.live/api/v1",
    Timeout = TimeSpan.FromSeconds(60),
    MaxRetries = 5,
    OrganizationId = "org_xxx"   // or set SENDLY_ORG_ID
});
```

`Timeout` defaults to 30 seconds and `MaxRetries` to 3. `OrganizationId` falls
back to the `SENDLY_ORG_ID` environment variable and is sent as the
`X-Organization-Id` header; `client.SetOrganizationId(...)` changes it later.

Network failures, timeouts, `408`s and `5xx` responses are retried up to
`MaxRetries` times with exponential backoff (1, 2, then 4 seconds), except
for `WhatsApp.Signup.VerifyAsync`, `WhatsApp.Signup.CreateAsync` with a
`BusinessAccountId` and `WhatsApp.Senders.UploadProfilePhotoAsync`, which
throw a `5xx`, a `408`, a timeout or a network failure at once (see
[WhatsApp](#whatsapp)). A `429` is
retried only when waiting can help; see [Rate Limits](#rate-limits). Every
other `4xx`, such as `400`, `401`, `402`, `403`, `404`, `409` or `422`, is
thrown on the first attempt, because the API gives the same answer to a
repeat. An id that is empty, `.` or `..` is refused with a
`ValidationException` before any request is sent.

## Messages

### Send an SMS

```csharp
// Marketing message (default)
var message = await client.Messages.SendAsync("+12025550143", "Check out our new features!");

// Transactional message (bypasses quiet hours)
var message = await client.Messages.SendAsync(new SendMessageRequest(
    "+12025550143",
    "Your verification code is: 123456"
) { MessageType = "transactional" });

// With custom metadata (max 4KB)
var message = await client.Messages.SendAsync(new SendMessageRequest(
    "+12025550143",
    "Your order #12345 has shipped!"
) { 
    Metadata = new Dictionary<string, object> 
    { 
        { "order_id", "12345" }, 
        { "customer_id", "cust_abc" } 
    } 
});

// Send from one of your owned numbers (or an alphanumeric sender ID).
// Omit From to use your default sender.
var message = await client.Messages.SendAsync(new SendMessageRequest(
    "+12025550143",
    "Hello from our team!"
) { From = "+447700900123" });

Console.WriteLine(message.Id);
Console.WriteLine(message.Status);
Console.WriteLine(message.CreditsUsed);
```

### Send an MMS

```csharp
// Attach media by URL (US/Canada)
var mms = await client.Messages.SendAsync(new SendMessageRequest(
    "+12025550143",
    "Here's your receipt"
) { MediaUrls = new List<string> { "https://acme.example/receipt.jpg" } });
```

Upload a local file first with [`client.Media`](#media) if you don't already
have a public URL.

### List Messages

```csharp
// Basic listing
var messages = await client.Messages.ListAsync();

foreach (var msg in messages)
{
    Console.WriteLine(msg.To);
}

// Filter by status and recipient, and page with Limit and Offset
var delivered = await client.Messages.ListAsync(new ListMessagesOptions
{
    Status = Message.Statuses.Delivered,
    To = "+12025550143",
    Limit = 50,
    Offset = 0
});

// Pagination info
Console.WriteLine(delivered.Total);   // matching messages, across all pages
Console.WriteLine(delivered.HasMore);
```

`Limit` defaults to 50 and is capped at 100 by the SDK before the request is
sent. A test key lists only sandbox messages, and a live key only live ones.

### Get a Message

```csharp
var message = await client.Messages.GetAsync("4a7c1e2f-9b3d-4c8a-91f2-7d5e6a0b3c19");

Console.WriteLine(message.To);
Console.WriteLine(message.Text);
Console.WriteLine(message.Status);
Console.WriteLine(message.DeliveredAt);
```

### Scheduling Messages

```csharp
// Schedule a message for future delivery
var scheduled = await client.Messages.ScheduleAsync(new ScheduleMessageRequest(
    "+12025550143",
    "Your appointment is tomorrow!",
    DateTime.UtcNow.AddDays(1).ToString("s") + "Z"
));

Console.WriteLine(scheduled.Id);
Console.WriteLine(scheduled.ScheduledAt);
Console.WriteLine(scheduled.CreditsReserved);

// List scheduled messages
var result = await client.Messages.ListScheduledAsync();
foreach (var msg in result)
{
    Console.WriteLine($"{msg.Id}: {msg.ScheduledAt}");
}

// Get a specific scheduled message
var one = await client.Messages.GetScheduledAsync("schd_xxx");

// Cancel a scheduled message (refunds credits)
var cancel = await client.Messages.CancelScheduledAsync("schd_xxx");
Console.WriteLine($"Refunded: {cancel.CreditsRefunded} credits");
```

The scheduled time must be ISO 8601 and between 5 minutes and 5 days in the
future; any other time is refused with a `400` `invalid_scheduled_time`
(`ValidationException`). `ScheduleMessageRequest` also has a parameterless
constructor, so `new ScheduleMessageRequest { To = ..., Text = ..., ScheduledAt = ... }`
works too.

### Batch Messages

```csharp
// Send multiple messages in one API call
var batch = await client.Messages.SendBatchAsync(new SendBatchRequest()
    .AddMessage("+12025550143", "Hello User 1!")
    .AddMessage("+12025550166", "Hello User 2!")
    .AddMessage("+12025550178", "Hello User 3!")
);

Console.WriteLine(batch.BatchId);
Console.WriteLine($"Sent: {batch.Sent}");
Console.WriteLine($"Failed: {batch.Failed}");
Console.WriteLine($"Skipped (opted out): {batch.OptedOutSkipped}");
Console.WriteLine($"Skipped (invalid): {batch.InvalidSkipped}");
Console.WriteLine($"Credits used: {batch.CreditsUsed}");
Console.WriteLine($"Credits refunded: {batch.CreditsRefunded}");

// Get batch status. QueuedCount and CreatedAt are reported only here and by the
// list endpoint, so they are null on the send response above.
var status = await client.Messages.GetBatchAsync(batch.BatchId);
Console.WriteLine($"Queued: {status.QueuedCount}");

// List all batches
var batches = await client.Messages.ListBatchesAsync();

// Preview batch (dry run) - validates without sending
var preview = await client.Messages.PreviewBatchAsync(new SendBatchRequest()
    .AddMessage("+12025550143", "Hello User 1!")
    .AddMessage("+447700900123", "Hello UK!")
);
Console.WriteLine($"{preview.Sendable} of {preview.Total} sendable, {preview.Duplicates} duplicates");
Console.WriteLine($"Credits needed: {preview.CreditsNeeded} (balance {preview.CreditBalance})");
foreach (var blocked in preview.BlockedMessages)
    Console.WriteLine($"#{blocked.Index} {blocked.To}: {blocked.Reason}");
if (!preview.CanSend)
    Console.WriteLine(string.Join("; ", preview.Warnings ?? new()));
```

`CanSend` is true when at least one message passes, the batch has at most
10,000 messages, every blocked message is an opt-out (a live send skips
those, but rejects the whole batch for any other block), the key has
`sms:send`, and the balance covers the batch or the key is a test key. The
preview does not check the monthly quota or a suspended workspace, and a test
send skips the destination checks the preview applies. `BlockReasons` counts
the blocked messages by reason, and `Compliance` holds the opt-out,
restricted-content and quiet-hours counts. `TotalMessages`, `WillSend`,
`CurrentBalance` and `HasEnoughCredits` hold the same values as `Total`,
`Sendable`, `CreditBalance` and `HasSufficientCredits`. The preview needs the
`sms:read` scope. It returns no per-message items, so `Messages` stays empty,
and its per-country breakdown (`byCountry`) has no property on the model.

Use `Sent`, not the queued count, to see how much of a batch actually went out.
A batch holds at most 10,000 messages. `Account.Limits.MaxBatchSize` is not
reported by the API; it always reads the SDK's built-in default of 1000.

### Iterate All Messages

```csharp
// Auto-pagination with IAsyncEnumerable
await foreach (var message in client.Messages.GetAllAsync())
{
    Console.WriteLine($"{message.Id}: {message.To}");
}

// With options
await foreach (var message in client.Messages.GetAllAsync(new ListMessagesOptions
{
    Status = "delivered"
}))
{
    Console.WriteLine($"Delivered: {message.Id}");
}
```

`GetAllAsync` requests pages of 100 (or your `Limit`) until the API reports no
more, so a loop over it sees every matching message.

### Group MMS

```csharp
// Send a group MMS to 2-8 US/Canada recipients. Everyone sees the others and
// replies fan out to the group. Omit From to use your default sender.
var group = await client.Messages.SendGroupAsync(new SendGroupMessageRequest(
    new[] { "+14155550142", "+14155550178" },
    "Hey team - quick sync at noon?"
));

Console.WriteLine(group.Id);              // "4a7c1e2f-9b3d-4c8a-91f2-7d5e6a0b3c19"
Console.WriteLine(group.GroupMessageId);  // "grp_xxx" (on live sends)
Console.WriteLine(group.Status);          // "sent" or "delivered"
Console.WriteLine(group.Simulated);       // true on a test key / before verification
Console.WriteLine(string.Join(", ", group.To)); // the recipients' numbers

// A live send also reports each recipient's status; null on a simulated send
foreach (var r in group.Recipients ?? new())
    Console.WriteLine($"{r.PhoneNumber}: {r.Status}"); // e.g. "queued"
```

The sending number must be an MMS-enabled, 10DLC-registered number you own.
Group MMS defaults to `transactional`; pass `messageType: "marketing"` to apply
quiet-hours rules.

### AI Message Enhancement

```csharp
// Rewrite a draft into a single polished SMS segment (<=160 chars).
var result = await client.Messages.EnhanceAsync(new EnhanceMessageRequest(
    text: "hey come check out our sale this weekend",
    messageType: "marketing"
));

Console.WriteLine(result.Enhanced);     // polished rewrite
Console.WriteLine(result.Explanation);  // what changed and why
Console.WriteLine(result.Model);        // model used, when reported
```

AI enhancement needs the `ai_classification` feature: while it is off for your
account the call throws `NotFoundException` (`not_found`). When the model call
itself fails, the response falls back to the original text with an empty
explanation.

## Idempotency

POSTs carry an automatically generated `Idempotency-Key`, reused on every one
of the SDK's own retries (after a timeout, a network error, a `5xx` or a
`429` it waits out), so a retry of a request that already reached the API
returns the original result instead of sending and charging again. The API
records a `2xx` or another `4xx` answer under the key, but never a `5xx` or a
`429`, so a retry after one of those runs the request again. Pass your own
key through the `IdempotentRequestOptions` overloads of `SendAsync`,
`SendGroupAsync`, `SendBatchAsync`, and
`ScheduleAsync` when the guarantee needs to outlive the process, such as a job
queue that re-runs after a crash. Reusing a key within 24 hours returns the
original response, and reusing it with a different body is rejected with a
`422` (`idempotency_key_mismatch`), so derive keys from something stable in
your domain, like an order id. `SendBatchAsync` sends no automatic key, because
the API already deduplicates identical batches by their contents. The RCS
registration writes (`client.Rcs.Brands` and `client.Rcs.Agents`) and the voice
writes (`client.Calls` and `client.Voice`) take an optional
`IdempotentRequestOptions` too; their `PATCH`, `PUT` and `DELETE` calls send a
key only when you supply one.

Keys are validated before any network call: 1 to 255 printable ASCII
characters, otherwise `ValidationException`. Empty and whitespace-only values
count as absent, so the automatic key still applies.

```csharp
var message = await client.Messages.SendAsync(
    new SendMessageRequest("+12025550143", "Your order has shipped!"),
    new IdempotentRequestOptions { IdempotencyKey = "order-4821-shipped" }
);
```

A key conflict is thrown as a `ValidationException` with `StatusCode` 422,
like other validation failures, so check
`ApiErrorCode == "idempotency_key_mismatch"` to tell it apart.

Full details: https://sendly.live/docs/idempotency

## Rate Limits

Requests are counted per API key in a fixed 60-second window that opens with the first request in it:

| Key | Requests per minute |
|-----|---------------------|
| Test (`sk_test_v1_*`) | 60 |
| Live (`sk_live_v1_*`) | 600 |
| Enterprise master key | 3000 |

Going over the limit returns `429` `rate_limit_exceeded` with a `Retry-After`
header. The client waits out a `429` and sends again, up to `MaxRetries` times,
only when waiting can help and the wait is 60 seconds or less: an ordinary
`rate_limit_exceeded`, the per-minute `provision_rate_limit` from enterprise
workspace provisioning, `too_many_concurrent_verifications` (too many
first-time API key checks at once, retried after 1 second), or a `429` with no
code. It sends again after exactly that wait, and throws without waiting after
the last attempt. Every other `429` is thrown on the first attempt as a
`RateLimitException`, including:

- `too_many_failed_key_attempts`: too many requests from this address used a
  wrong API key for the account, so its keys, even a correct one, are refused
  from this address for up to 300 seconds. Fix the key rather than retrying.
- `max_attempts_exceeded` from `Verify.CheckAsync`, `daily_call_limit`,
  `quota_exceeded`, and the hourly `provision_rate_limit` while more than a
  minute of its window remains.
- `Verify.SendAsync` and `ResendAsync` against the per-phone limit (5 codes per
  10 minutes) or the daily limit (20 per day), while more than a minute of
  the limit remains.

`RateLimitException.RetryAfter` (a `TimeSpan?`) is the `Retry-After` header,
or the body's `retryAfter` when there is no header, and `ApiErrorCode` holds
the code:

```csharp
try
{
    await client.Messages.SendAsync("+12025550143", "Hello!");
}
catch (RateLimitException e) when (e.ApiErrorCode == "too_many_failed_key_attempts")
{
    Console.WriteLine("This address is locked out after wrong API keys: check the key, do not retry.");
}
catch (RateLimitException e)
{
    Console.WriteLine($"{e.ApiErrorCode}: retry after {e.RetryAfter?.TotalSeconds} seconds");
}
```

## Message Templates

Reusable SMS templates with `{{variables}}`, published for use with the Verify
API. (`client.Templates` is a second, slimmer view of the same `/templates`
endpoint.)

```csharp
// List presets + custom templates
var listing = await client.MessageTemplates.ListAsync();
foreach (var t in listing.Templates)
    Console.WriteLine($"{t.Name} ({t.Status}, preset: {t.IsPreset})");

// Presets only
var presets = await client.MessageTemplates.PresetsAsync();

// Create a draft and edit it while it is still a draft, then publish
// (a published template can no longer be updated)
var template = await client.MessageTemplates.CreateAsync(new CreateMessageTemplateRequest
{
    Name = "Password Reset",
    Text = "{{app_name}}: your reset code is {{code}}. Valid for 10 minutes."
});
await client.MessageTemplates.UpdateAsync(template.Id, new UpdateMessageTemplateRequest
{
    Text = "{{app_name}}: your reset code is {{code}}. Valid for 15 minutes."
});
await client.MessageTemplates.PublishAsync(template.Id);

// Read it back
var one = await client.MessageTemplates.GetAsync(template.Id);

// Preview with sample values
var preview = await client.MessageTemplates.PreviewAsync(template.Id, new Dictionary<string, string>
{
    ["app_name"] = "MyApp",
    ["code"] = "123456"
});
Console.WriteLine(preview.PreviewText);
Console.WriteLine($"{preview.CharacterCount} characters, {preview.SegmentCount} segments");

// AI-generate a template from a description
var generated = await client.MessageTemplates.GenerateAsync(new GenerateMessageTemplateRequest
{
    Description = "Tell a customer their order is out for delivery"
});
Console.WriteLine(generated.Text);

await client.MessageTemplates.DeleteAsync(template.Id);
```

> **Known gaps.** `client.Templates.UnpublishAsync` is marked `[Obsolete]`
> because the API has no unpublish route, so it answers `404`. (`CloneAsync` on
> both resources works: it copies the template into a new draft and needs the
> `templates:write` scope.) To retire a published template, publish a
> replacement and delete the old one. `ListTemplatesOptions.Limit`, `Type` and
> `Locale` are sent but ignored by the list endpoint, which returns every
> template.

## Branded Short Links

Mint branded, owned-domain short links (better carrier deliverability than
public shorteners) with click analytics and a per-link kill switch.

> **Note:** URL shortening is gated behind the founder-only `url_shortener`
> flag and is not yet publicly stable. Calls raise `NotFoundException`
> (`not_found`) until the flag is on for your account.

```csharp
// Shorten a URL
var link = await client.Links.CreateAsync("https://acme.example/spring-sale");
Console.WriteLine(link.ShortUrl); // "https://sendly.live/l/Ab3xY7"
Console.WriteLine(link.Code);     // "Ab3xY7"

// List your links with click counts (limit 1-200, default 50)
var links = await client.Links.ListAsync(new ListShortLinksOptions { Limit = 20 });
foreach (var l in links.Links)
{
    Console.WriteLine($"{l.ShortUrl} -> {l.DestinationUrl} ({l.ClickCount} clicks)");
    Console.WriteLine(string.Join(",", l.Spark)); // 14-day daily histogram
}

// Kill / re-enable a link
await client.Links.DisableAsync(link.Code);
await client.Links.EnableAsync(link.Code);
```

## Numbers

```csharp
// Browse coverage and search for an available number
var countries = await client.Numbers.ListCountriesAsync();
foreach (var c in countries.Countries)
    Console.WriteLine($"{c.Code} {c.Name}: {string.Join(", ", c.NumberTypes)}");

var available = await client.Numbers.ListAvailableAsync(new ListAvailableNumbersOptions
{
    Country = "GB",
    Type = "mobile"
});

// List the numbers you own
var owned = await client.Numbers.ListAsync();
foreach (var n in owned.Numbers)
{
    Console.WriteLine($"{n.PhoneNumber} — {n.Status}");
}

// Get one by id (includes IsDefault)
var number = await client.Numbers.GetAsync("num_xxx");
Console.WriteLine($"{number.PhoneNumber} — default: {number.IsDefault}");

// Make it the workspace default sender (must be active)
var updated = await client.Numbers.UpdateAsync("num_xxx", new UpdateNumberRequest { IsDefault = true });
// (or the convenience wrapper)
await client.Numbers.SetDefaultAsync("num_xxx");

// Cancel a scheduled release ("keep this number")
await client.Numbers.UpdateAsync("num_xxx", new UpdateNumberRequest { PendingCancellation = false });
await client.Numbers.KeepAsync("num_xxx"); // convenience wrapper

// Release a number. A live paid purchase is cancelled at period end.
var release = await client.Numbers.ReleaseAsync("num_xxx");
if (release.Scheduled == true)
    Console.WriteLine($"Releases at {release.ScheduledReleaseAt}");
else
    Console.WriteLine("Released");
```

`OwnedNumber.MonthlyCostCents` reads 0 for a number with no recorded monthly
price, such as the toll-free number provisioned with a verification;
`MonthlyCostCentsOrNull` is null for such a number, which tells it apart from
a free one.

### Buying a number

A purchase either completes straight away or hands back a hosted action the
user has to finish. When `Status` is `documents_required` or
`payment_required`, `Action` carries a Sendly-hosted URL plus a short code to
show them; once they are done, call `BuyAsync` again with the **same** body
plus `ActionCode` set to that action's `ActionCode`.

```csharp
var first = available.Numbers[0];
var result = await client.Numbers.BuyAsync(new BuyNumberRequest
{
    PhoneNumber = first.PhoneNumber,
    CountryCode = first.Country,
    PhoneNumberType = first.NumberType,
    MonthlyCost = first.MonthlyCost,   // echo the priced value back
});

if (result.Action != null)
{
    Console.WriteLine($"Send your user to {result.Action.Url}, code {result.Action.Code}");

    // ...after they finish:
    result = await client.Numbers.BuyAsync(new BuyNumberRequest
    {
        PhoneNumber = first.PhoneNumber,
        CountryCode = first.Country,
        PhoneNumberType = first.NumberType,
        MonthlyCost = first.MonthlyCost,
        ActionCode = result.Action.ActionCode,
    });
}

Console.WriteLine(result.Status);            // "provisioning" once accepted
Console.WriteLine(result.Number?.PhoneNumber);
```

`Action.Code` is the 8-character code the user types on the hosted page;
`Action.ActionCode` is the 32-hex identifier you pass back. They are not
interchangeable.

## 10DLC Registration

Register your business for carrier review so you can text from local
(10-digit) US numbers. Brand, campaign and assignment writes need a live key.

```csharp
using Sendly.Resources;

// 1. Register a brand, then poll until it is verified
var brand = (await client.TenDlc.CreateBrandAsync(new CreateTenDlcBrandRequest
{
    LegalName = "Acme Holdings LLC",
    Ein = "12-3456789",
    Website = "https://acme.example",
    Email = "ops@acme.example",
})).Data;

var check = (await client.TenDlc.GetBrandAsync(brand.Id)).Data;
Console.WriteLine(check.Status); // "pending" -> "verified" / "failed"

// 2. Pre-check the use case, then create a campaign
var qualify = (await client.TenDlc.QualifyAsync(brand.Id, "MIXED")).Data;
if (qualify.Qualified)
{
    var campaign = (await client.TenDlc.CreateCampaignAsync(new CreateTenDlcCampaignRequest
    {
        BrandId = brand.Id,
        UseCase = "MIXED",
        Description = "Order updates and support replies for Acme customers",
        MessageFlow = "Customers opt in at checkout on acme.example",
        SampleMessages = new() { "Your order 123 has shipped" },
    })).Data;

    // poll GetCampaignAsync until Status == "active", then:
    var assignment = (await client.TenDlc.AssignNumberAsync(campaign.Id, "+12025550143")).Data;
    Console.WriteLine(assignment.Status); // sendable once "Active"
}

var brands = await client.TenDlc.ListBrandsAsync();
var campaigns = await client.TenDlc.ListCampaignsAsync();
var assignments = await client.TenDlc.ListAssignmentsAsync();
```

Every 10DLC response wraps its payload in `Data`. Brand statuses are `pending`,
`verified` and `failed` (with `FailureReasons`); campaign statuses are
`awaiting_review` (held in Sendly's review queue before it reaches the
carrier), `changes_requested`, `pending`, `active`, `failed`, `suspended` and
`expired`; assignment statuses are `Active`, `Under review` and `Action needed`.

## Short Codes

> **This SDK has no short-code helpers.** There is no `client.ShortCodes`
> resource and no short-code model. Everything below is plain REST — call it
> with your own HTTP client, or reach it through the Sendly CLI or dashboard.

Short-code applications live at these endpoints, authenticated with the same
`Authorization: Bearer <api key>` header this client uses:

| Method | Path | Scope |
|--------|------|-------|
| `GET` | `/api/v1/short_codes` | `short_codes:read` |
| `POST` | `/api/v1/short_codes/requests` | `short_codes:write` |
| `GET` | `/api/v1/short_codes/application` | `short_codes:read` |
| `PUT` | `/api/v1/short_codes/application` | `short_codes:write` |
| `POST` | `/api/v1/short_codes/application/preflight` | `short_codes:read` |
| `POST` | `/api/v1/short_codes/application/submit` | `short_codes:write` |

You can still follow an application from C# through webhooks:
`Webhook.EventTypes.ShortCodeActionRequired`, `ShortCodeRejected`,
`ShortCodeFiled` and `ShortCodeLive` arrive like any other lifecycle event and
decode with `ObjectAs<T>()`.

## Business Upgrade

The toll-free entity-upgrade ("fork-with-new-number") flow: provision a new
toll-free number and messaging profile under a new legal entity, keep sending
on the old number through the review window, and swap on approval.

```csharp
using Sendly.Resources;

var report = await client.BusinessUpgrade.PreflightAsync(candidate); // advisory, no writes
Console.WriteLine(report.Verdict);

var prefill = await client.BusinessUpgrade.BestPrefillAsync();
var started = await client.BusinessUpgrade.StartAsync(workspaceId, parameters, einDocument);

var status = await client.BusinessUpgrade.StatusAsync(workspaceId); // Pending is null when none
await client.BusinessUpgrade.ResubmitAsync(workspaceId, parameters, einDocument);
await client.BusinessUpgrade.CancelAsync(workspaceId);

// On approval, decide what happens to the old number
await client.BusinessUpgrade.SetDispositionAsync(workspaceId, new DispositionRequest
{
    Disposition = "released",   // or "moved", with TargetWorkspaceId
});
```

`StartAsync` and `ResubmitAsync` are multipart uploads; the EIN document
accepts bytes, a stream or a path via `EinDocumentInput`.

## WhatsApp

Connect a number you own to WhatsApp ($19 one-time setup, no monthly fee),
create Meta-reviewed message templates, and send with
`client.Messages.SendAsync(new SendWhatsAppMessageRequest(...))`. Free-form
text and media only deliver inside an open 24-hour customer-service window
(the recipient messaged you in the last 24h); an approved template works
anytime.

Sends go through `Messages.SendAsync` with a `SendWhatsAppMessageRequest`
(`POST /v1/messages` with channel `whatsapp`) and need `sms:send`, not
`whatsapp:write`. Reads (`Signup.GetAsync`, templates, the window, senders,
sender profiles and conversational components) need `whatsapp:read` and
accept test keys. Signup (including `VerifyAsync` and `ResendAsync`), template
create/edit/delete and sender edits (profile, photo, conversational components
and calling) need `whatsapp:write` and a live key (otherwise 403
`whatsapp_requires_live_key`). Sends need a live key too. In a team workspace,
connecting and sender edits need an owner or admin (`settings:write`), and
template writes need an owner, admin or member (`templates:write`). A missing
role returns 403 `insufficient_permissions`.

WhatsApp is enabled per person: the user who owns the API key, not the
workspace. While it is off, the `/api/v1/whatsapp/*` management routes
answer `404` (`not_found`, a `NotFoundException`) and a WhatsApp send is
refused with `403` `whatsapp_not_enabled` (a `SendlyException`).

```csharp
// 1. Connect a number. The connect URL must be opened by a human: they log
//    in with Facebook in a browser to link their WhatsApp Business Account.
var signup = await client.WhatsApp.Signup.CreateAsync("+12025550181");
Console.WriteLine($"Have your user open: {signup.ConnectUrl}");

// 2. Poll until active. After the Facebook step the signup stays
//    "registering" while WhatsApp activates the number. Activation usually
//    takes a few minutes but can take hours. If it hasn't finished about 6
//    hours after the session began, the session fails with
//    registration_timeout and the fee is refunded. "failed" carries a reason
//    in FailureReasons; if the connection fails, the $19 fee is refunded
//    automatically.
var status = await client.WhatsApp.Signup.GetAsync(signup.Id);
Console.WriteLine(status.Status); // "initiated" -> "registering" -> "active"

// List your connected senders
var senders = await client.WhatsApp.Senders.ListAsync();
foreach (var s in senders.Senders)
{
    Console.WriteLine($"{s.PhoneNumber} ({s.DisplayName ?? "no name yet"}) — {s.Status}");
}

// 3. Create a template (Meta reviews it, usually 24-48h)
var template = await client.WhatsApp.Templates.CreateAsync(new CreateWhatsAppTemplateRequest
{
    Sender = "+12025550181",
    Name = "order_shipped",
    Language = "en_US",
    Category = "UTILITY",
    Body = "Hi {{1}}, your order {{2}} has shipped!",
    Examples = new() { ["1"] = "Sam", ["2"] = "#4821" }
});
Console.WriteLine(template.Status); // "PENDING"

// List templates; edit a rejected one and resubmit (template names are locked
// for ~30 days after deletion, so editing is the recovery path)
var templates = await client.WhatsApp.Templates.ListAsync();
await client.WhatsApp.Templates.UpdateAsync(template.Id, new UpdateWhatsAppTemplateRequest
{
    Body = "Hi {{1}}, your order {{2}} is on its way!",
    Examples = new() { ["1"] = "Sam", ["2"] = "#4821" }
});
await client.WhatsApp.Templates.DeleteAsync("wat_xxx");

// 4. Check the 24-hour window (your sender first, then the recipient). The
//    response is exactly { open, expiresAt }: no window on record gives Open
//    false and ExpiresAt null; an expired one gives Open false and the past
//    expiry.
var window = await client.WhatsApp.WindowAsync("+12025550181", "+12025550143");
Console.WriteLine($"{window.Open} until {window.ExpiresAt}");

// Free-form text inside an open window
var message = await client.Messages.SendAsync(new SendWhatsAppMessageRequest(
    "+12025550143",
    "+12025550181",
    text: "Your table is ready!"
));

// Media with a caption (window-bound; WhatsApp accepts exactly one attachment)
await client.Messages.SendAsync(new SendWhatsAppMessageRequest(
    "+12025550143",
    "+12025550181",
    text: "Here's your receipt",
    mediaUrls: new() { "https://acme.example/receipt.pdf" }
));

// Template send — works regardless of the window
await client.Messages.SendAsync(new SendWhatsAppMessageRequest(
    "+12025550143",
    "+12025550181",
    template: new WhatsAppTemplateSendParams
    {
        Name = "order_shipped",
        Language = "en_US",
        Variables = new() { ["1"] = "Sam", ["2"] = "#4821" }
    }
));

Console.WriteLine(message.WhatsApp.Kind); // "text", "media", or "template"
Console.WriteLine(message.CreditsUsed);   // a reply in the window: see pricing below

// 5. Read and edit a sender's business profile — the contact card recipients
//    see. Supply only the fields to change; omitted fields keep their value.
var profile = await client.WhatsApp.Senders.GetProfileAsync("+12025550181");
Console.WriteLine($"{profile.DisplayName} — {profile.About}");

await client.WhatsApp.Senders.UpdateProfileAsync("+12025550181",
    new UpdateWhatsAppSenderProfileRequest
    {
        About = "Fast delivery, friendly service",  // max 139 chars
        Description = "Acme sells everything.",     // max 512 chars
        Website = "https://acme.example"
    });
```

### Add a number to a connected account

Once one number has connected through Facebook, more numbers can join the
same WhatsApp Business account from code. Meta sends the new number a 6-digit
code by text (`sms`, the default) or voice call (`voice`); the same $19
one-time fee applies, refunded automatically if the connection fails. A null
`BusinessAccountId` starts a Facebook connection instead, with its own $19
fee (a blank one throws a `ValidationException`), so take it from an active sender (the list is newest first, and
a pending sender has none).

```csharp
var accountId = senders.Senders.First(s => s.Status == "active" && s.BusinessAccountId != null).BusinessAccountId;

var adding = await client.WhatsApp.Signup.CreateAsync(new StartWhatsAppSignupRequest
{
    PhoneNumber = "+12025550182",
    BusinessAccountId = accountId,
    VerificationMethod = WhatsAppVerificationMethod.Sms, // or Voice
    DisplayName = "Acme"                                 // optional
});
Console.WriteLine(adding.Status); // "verifying" (no ConnectUrl)

// A code sent by text arrives on the number like any inbound message, so
// GetAsync returns it once it has landed
var pending = await client.WhatsApp.Signup.GetAsync(adding.Id);
if (pending.VerificationCode is { } code)
{
    var connected = await client.WhatsApp.Signup.VerifyAsync(adding.Id, code);
    Console.WriteLine(connected.Status); // "active"
}

// No code yet? Ask for another one, at most every 30 seconds
await client.WhatsApp.Signup.ResendAsync(adding.Id, WhatsAppVerificationMethod.Voice);
```

Until a code has been submitted, `VerificationCode` is the newest code that
has arrived since the signup started, so after a resend it still shows the
earlier code until the new one arrives. Once WhatsApp has checked a code, only
a code that arrived after the last submission or resend is returned. A
submission answered with `502` `whatsapp_verification_unavailable` is not
counted, so the same unchecked code can come back, and submitting it again is
safe.

A wrong code is a `422` `whatsapp_verification_code_invalid`
(`ValidationException`) whose body has `attemptsRemaining`; after 5 wrong codes
the signup fails with `409` `whatsapp_verification_failed` and the fee is
refunded. Other refusals: `404` `whatsapp_business_account_not_found` (no
connected account with that id in the workspace), `400`
`display_name_required`, `422` `whatsapp_verification_start_failed` (WhatsApp
refused to verify the number; the fee is refunded), `502`
`whatsapp_verification_start_failed` (WhatsApp couldn't be reached; the fee is
refunded, so start again), `409` `whatsapp_verification_busy` (another code
is being checked; try again), `502` `whatsapp_verification_unavailable` (the
attempt isn't counted; try again), `502` `whatsapp_activation_pending` (the
code was accepted but connecting didn't finish; check back), `409`
`signup_not_active`, and `429` `whatsapp_verification_resend_too_soon`, thrown
at once with `RetryAfter`. `CreateAsync` with a `BusinessAccountId` and
`VerifyAsync` throw a `5xx`, a `408`, a timeout or a network failure
(`NetworkException`) on the first attempt and never retry it, because the
outcome is unknown: a retried start could begin a new session that is charged
and, when it fails, refunded, and every code submission uses up one of the 5
attempts (after `whatsapp_activation_pending` WhatsApp has already accepted the
code, so check `GetAsync` before submitting again). Only a `429` the client
waits out is retried. `ResendAsync` retries like any other call.
New failure reasons are `verification_start_failed`,
`verification_failed` and `verification_expired`.

### Profile photo, conversation starters and calling

```csharp
// Profile photo: a square JPEG or PNG, at most 5 MB, at least 192 px wide
await client.WhatsApp.Senders.UploadProfilePhotoAsync("+12025550181", "logo.png");
await client.WhatsApp.Senders.DeleteProfilePhotoAsync("+12025550181");

// Ice breakers (up to 4) show when someone opens a chat with you for the
// first time; commands (up to 30) show when they type "/". Each list you set
// replaces the stored one, an empty list clears it, a null list is kept.
await client.WhatsApp.Senders.UpdateConversationalComponentsAsync("+12025550181",
    new UpdateWhatsAppConversationalComponentsRequest
    {
        IceBreakers = new() { "What are your hours?", "Track my order" },
        Commands = new() { new WhatsAppCommand { Command = "menu", Description = "See today's menu" } }
    });
var components = await client.WhatsApp.Senders.GetConversationalComponentsAsync("+12025550181");

// WhatsApp calling: WhatsApp users who call the number ring like a phone
// call (dashboard or AI agent, per the number's voice settings)
var calling = await client.WhatsApp.Senders.SetCallingAsync("+12025550181", true);
Console.WriteLine($"{calling.CallingEnabled} {calling.OutboundCallingAllowed}");
```

The photo is checked by its bytes: anything but a JPEG or PNG is a `400`
`whatsapp_profile_photo_invalid`, and over 5 MB a `413`
`whatsapp_profile_photo_too_large`. A component list that breaks a rule is a
`400` `invalid_request` whose message names the rule. Turning calling on needs
voice switched on for the number first (`409` `voice_not_enabled`; see
[Configure voice](#configure-voice)); Meta allows it only once the account
may message 2,000 people a day and the display name is approved (`422`
`whatsapp_calling_unavailable`). A WhatsApp call to your number is billed at
the normal inbound rate. `Senders.ListAsync` reports each sender's
`BusinessAccountId`, `BusinessName`, `CallingEnabled` and
`OutboundCallingAllowed` (false for +1, +20, +84 and +234 numbers, where Meta
forbids business-initiated calls). There is no API for placing WhatsApp
calls. Carrier failures are `502`s (`whatsapp_profile_update_failed`,
`whatsapp_conversational_components_fetch_failed`,
`whatsapp_conversational_components_update_failed` and
`whatsapp_calling_update_failed`). The client retries them like any `5xx`
before throwing, except for the photo upload, which throws a `5xx`, a `408`,
a timeout or a network failure on the first attempt.

Templates are categorized `AUTHENTICATION`, `UTILITY` or `MARKETING`, and the
category drives Meta's review rules. `Category` is required on create, with
no default (leaving it out returns a `400` `template_category_invalid`), and
an update can't change it.

Pricing: free-form text or media inside the 24-hour window costs 1 credit
each for the first 1,000 per sending number per calendar month (UTC), then
the destination's utility template price; countries without a listed price
use the default utility price of 12 credits. Templates are priced by category
and destination country; countries without a listed price use 33
(marketing), 12 (utility) and 12 (authentication) credits. A failed send
gives its slot back. Meta has paused
marketing template delivery to US (+1) numbers. A template `Header` is fixed
text: one containing `{{n}}` is refused with a `400`
`template_header_variable_unsupported`, because sends fill only body and
button variables. The other template pre-flight refusals are `400`s too:
`template_authentication_otp_button_required` and
`template_authentication_no_links` (a link in the body or a URL button on an
authentication template). On create, `404` `whatsapp_sender_not_connected`
is checked first. A marketing template without an opt-out button only gets a
warning.

Refusals carry the API's code in `ApiErrorCode`. `whatsapp_window_closed`
(`422`) means the window is closed, so send a template. `whatsapp_send_failed`
is a `422` (`ValidationException`) when WhatsApp refused the message, which is
final (cached under the idempotency key and replayed for 24 hours), or a
`502` when the message provably never reached the carrier, so it was not sent
and is safe to send again; a `502` is never cached, and the client retries it
like any `5xx` under the same idempotency key. Neither is charged. A `409`
`whatsapp_send_unconfirmed` (`SendlyException`) means the outcome is unknown:
the message was marked failed and refunded but may still be delivered, so
check before sending it again (it could arrive twice). It is not retried
automatically. Only connecting can fail with `503` `whatsapp_unavailable`; no send
returns it (nothing is charged; the client retries it, and the body's
`retryAfter: 3600` and the `Retry-After: 3600` header say how long to wait),
or `429`
`whatsapp_signup_limit_reached` after 5 failed, charged signups in 24 hours,
which is thrown at once; try again the next day. Once a number has
connected there is no refund, and a later disconnect gets nothing back.

## RCS

Send branded rich messaging — cards and suggestion chips — with
`client.Messages.SendAsync(new SendRcsMessageRequest(...))`. Plain-text RCS
sends fall back to SMS automatically for recipients whose device can't receive
RCS. RCS is gated behind the `rcs_channel` rollout flag (default-dark): while
it is off for your account every `client.Rcs` call throws `NotFoundException`
(`ApiErrorCode` `rcs_not_enabled`, or `not_found` from `Agents.ListAsync` and
`CapabilityAsync`). Sends and capability checks require a live
API key.

### Register your brand and agent

Sending as your brand requires an RCS agent — the verified sender identity
recipients see. Registration is self-serve, from the dashboard or the API:
draft a brand and an agent, submit them to Sendly for review, and Sendly passes
them to the carrier network. Reads need an API key with the `rcs:read` scope,
writes `rcs:write`. Logo, hero and call-to-action media must already be public
`https://` URLs; uploading files is dashboard-only. US businesses only for now.

```csharp
// 1. Draft the brand. Dossier.GetAsync() prefills it from details already on
//    file (10DLC or toll-free verification); fill in what's missing.
var dossier = await client.Rcs.Dossier.GetAsync();
var brandInput = dossier.Brand;
brandInput.DisplayName = "Acme Coffee";
brandInput.LegalEntityType ??= RcsLegalEntityType.LimitedLiabilityCompany;
brandInput.WebsiteUrl ??= "https://acme.example";
var brand = (await client.Rcs.Brands.CreateAsync(brandInput)).Brand;

// 2. Draft the agent under it
var agent = (await client.Rcs.Agents.CreateAsync(new CreateRcsAgentRequest
{
    BrandId = brand.Id,
    DisplayName = "Acme Coffee",
    UseCase = RcsAgentUseCase.MultiUse,
    Basics = new RcsAgentBasicsInput
    {
        Description = "Order updates and support for Acme Coffee customers",
        LogoUrl = "https://acme.example/rcs/logo.png",
        HeroUrl = "https://acme.example/rcs/hero.png",
        BrandColor = "#0B6E4F",
        PrivacyPolicyUrl = "https://acme.example/privacy",
        TermsAndConditionsUrl = "https://acme.example/terms",
        Website = new RcsAgentWebsiteContact { Url = "https://acme.example", Label = "Visit our site" }
    }
})).Agent;

// 3. Submit for review. Required-field checks run here; a 422 lists each gap
//    in ValidationException.FieldErrors as brand.<field> / agent.<field>.
var submitted = await client.Rcs.Agents.SubmitAsync(agent.Id,
    new IdempotentRequestOptions { IdempotencyKey = $"rcs-submit-{agent.Id}" });
Console.WriteLine(submitted.Stage);  // "in_review"

// 4. Poll until the stage reaches "testing", then invite your devices and
//    fill in the campaign
var status = await client.Rcs.Agents.GetAsync(agent.Id);
if (status.Stage == RcsCustomerStage.Testing)
{
    await client.Rcs.Agents.SetTestDevicesAsync(agent.Id, new[]
    {
        new RcsTestDeviceInput("+13125550100", "Sam Pixel")
    });

    await client.Rcs.Agents.UpdateAsync(agent.Id, new UpdateRcsAgentRequest
    {
        Campaign = new RcsCampaign
        {
            AgentOverview = "Order confirmations, pickup alerts, and support replies",
            Interactions = new()
            {
                new RcsInteraction { InteractionType = RcsInteractionType.TransactionalUpdates, Description = "Order status" }
            },
            MessageExamples = new()
            {
                "Your order #4821 is being roasted.",
                "Your order #4821 is ready for pickup!",
                "Thanks for visiting. Reply HELP for support."
            },
            ConsentSettings = new RcsConsentSettings
            {
                OptInMethods = new() { new RcsOptInMethod { MethodType = RcsOptInMethodType.Website, Description = "Checkout checkbox" } },
                CallToAction = "Text me order updates",
                CallToActionUrl = "https://acme.example/checkout",
                OptInMessage = "Welcome to Acme Coffee updates. Reply STOP to opt out.",
                HelpResponse = "Acme Coffee: email help@acme.example for support.",
                OptOutResponse = "You have been unsubscribed from Acme Coffee updates."
            }
        }
    });

    // 5. Once you've tested on an invited device, request launch
    var launch = await client.Rcs.Agents.RequestLaunchAsync(agent.Id,
        new RcsRequestLaunchRequest { TestUrl = "https://acme.example/rcs-test" });
    Console.WriteLine(launch.Stage);  // "launch_review"
}

// The whole registration at a glance
var registration = await client.Rcs.Registration.GetAsync();
Console.WriteLine($"{registration.Stage}: {registration.Agent?.DisplayName}");
```

Updates only change the fields you set: leave a property `null` to keep its
value, send an empty string to clear a text field, and set
`UpdateRcsAgentRequest.ClearCampaign` / `ClearTesting` to remove a whole
section. Registration errors carry the API's code in
`SendlyException.ApiErrorCode` (`RcsErrorCode` lists them): `rcs_field_locked`,
`rcs_brand_not_verified` and `rcs_launch_not_ready` are 409s (`SendlyException`
with `StatusCode` 409), `rcs_us_only` and `rcs_invalid_content` are 422s
(`ValidationException`, with `FieldErrors` on the latter), and `rcs_not_found`
is a 404 (`NotFoundException`).

Stage values are plain strings on `RcsCustomerStage`, in journey order:
`draft`, `in_review`, `changes_requested`, `rejected`, `brand_verification`,
`agent_review`, `testing`, `launch_review`, `launching`, `launch_rejected`,
`live`, `suspended`, `failed`. Sendly's own review state is tracked separately
on `RcsReviewStatus` (`draft`, `awaiting_review`, `changes_requested`,
`approved_for_carrier`, `rejected`, `launch_requested`, `launch_submitted`,
`launch_rejected`, `failed`).

### Send

```csharp
// 1. Find your agent — the brand identity your messages are sent as.
//    "testing" reaches invited test numbers only, "approved" reaches everyone.
var agents = await client.Rcs.Agents.ListAsync();
foreach (var agent in agents.Agents)
{
    Console.WriteLine($"{agent.Name} ({agent.Status}, stage={agent.Stage}, sendable={agent.Sendable})");
}

// 2. Optional pre-flight: can this recipient receive RCS?
var capability = await client.Rcs.CapabilityAsync("+12025550143");
Console.WriteLine(capability.Capable);  // false -> text falls back to SMS
Console.WriteLine(string.Join(", ", capability.Features));

// 3. Send text, optionally with suggestion chips. A reply chip's tap comes
//    back as an inbound message carrying your postbackData; an action chip
//    opens a URL.
var message = await client.Messages.SendAsync(new SendRcsMessageRequest(
    "+12025550143",
    text: "Your order #4821 has shipped!",
    suggestions: new()
    {
        RcsSuggestion.CreateReply("Thanks", "thanks"),
        RcsSuggestion.CreateAction("Track", "track", "https://acme.example/track/4821")
    }
));

// The response tells you which leg delivered
if (message.FellBackToSms)
{
    // Not RCS-capable: sent and billed as SMS, chips dropped
    Console.WriteLine(message.Channel);                  // "sms"
    Console.WriteLine(message.Rcs.RequestedChannel);     // "rcs"
    Console.WriteLine(message.Rcs.SuggestionsDropped);   // True
}
else
{
    Console.WriteLine(message.Channel);        // "rcs"
    Console.WriteLine(message.Rcs.Kind);       // "text"
    Console.WriteLine(message.Rcs.AgentName);  // "Acme Coffee"
}

// 4. Send a rich card. Cards have no SMS form — a card to a non-RCS recipient
//    fails with rcs_not_supported_for_recipient rather than falling back.
await client.Messages.SendAsync(new SendRcsMessageRequest(
    "+12025550143",
    card: new RcsCard
    {
        Title = "Order #4821 shipped",
        Description = "Arriving Thursday",
        MediaUrl = "https://acme.example/package.jpg",  // public JPEG, PNG, or GIF
        Orientation = "vertical",                       // or "horizontal"
        Suggestions = new()
        {
            RcsSuggestion.CreateAction("Track", "track", "https://acme.example/track/4821")
        }
    }
));

// Opt out of the SMS fallback to get a 422 instead of an SMS charge
await client.Messages.SendAsync(new SendRcsMessageRequest(
    "+12025550143",
    text: "RCS only, please",
    fallbackToSms: false
));

// Pass agentId when your workspace has more than one agent (otherwise the
// send fails with rcs_agent_ambiguous)
await client.Messages.SendAsync(new SendRcsMessageRequest(
    "+12025550143",
    text: "Your order #4821 has shipped!",
    agentId: "rag_abc123"
));
```

## Voice Calls

Place phone calls handled by your AI agents (the receptionists you create with
`client.Voice.Agents` or in the dashboard under Calls → Agents), list and
inspect calls, end a call, and download recordings. Over the API a call is
always answered by one of your agents; the agent speaks first and follows any
`Context` you attach. Reads need an API key with the `calls:read` scope, writes
`calls:write` and a live key.

Calls are prepaid from your credit balance per started minute: an agent-handled
outbound call costs 10 credits a minute ($0.10, being 2 for the call plus 8 for
the agent), an inbound call 2 credits a minute on a local number or 3 on a
toll-free one, plus 8 when an agent answers. Unanswered calls cost nothing, and
browser-to-browser calls between teammates are free. Destinations are US and
Canada. The number you call from must have voice enabled and an emergency
address registered; see [Configure voice](#configure-voice) to do both from
code.

> **Note:** Voice is being enabled workspace by workspace. Until it is on for
> yours, every `client.Calls` and `client.Voice` call throws
> `NotFoundException` (`ApiErrorCode` `voice_not_enabled`).

```csharp
using Sendly.Resources;

// Place a call handled by an agent
var call = await client.Calls.CreateAsync(new CreateCallRequest
{
    To = "+12025550143",
    AgentId = "3c4d5e6f-7081-4293-a4b5-c6d7e8f90a1b",
    From = "+15125550123",                        // optional with one voice-enabled number
    Context = "You are calling Jordan to confirm the 3pm appointment on Tuesday.",
    Metadata = new() { ["crmId"] = "lead_8812" }, // echoed on every read and webhook
});
Console.WriteLine($"{call.Id} {call.Status}"); // "... ringing"

// List calls, newest first
var calls = await client.Calls.ListAsync(new ListCallsOptions
{
    Status = CallStatus.Completed,
    Direction = CallDirection.Outbound,
    Limit = 20,
});
foreach (var c in calls.Data)
    Console.WriteLine($"{c.From} -> {c.To} {c.DurationSecs}s {c.CreditsCharged} credits ({c.HangupClass})");
if (calls.Pagination.HasMore)
    Console.WriteLine($"{calls.Pagination.Total - calls.Data.Count} more");

// Get one call; agent calls include the transcript
call = await client.Calls.GetAsync(call.Id);
foreach (var line in call.Transcript ?? new())
    Console.WriteLine($"[{line.AtMs}ms] {line.Speaker}: {line.Text}");

// End a call early (ringing -> cancelled, active -> completed)
call = await client.Calls.HangupAsync(call.Id);

// Download the recording (signed URL, valid for five minutes)
var recording = await client.Calls.RecordingAsync(call.Id);
if (recording.Status == CallRecordingStatus.Ready)
    Console.WriteLine($"{recording.Url} until {recording.ExpiresAt}"); // audio/ogg
```

`Context` is capped at 2000 characters and is not echoed back. `Metadata`
accepts up to 20 key/value pairs (keys 1-40 characters of letters, digits and
`_ . : -`; values up to 500 characters). Recording is switched on per workspace
in the dashboard under Calls → Settings; agent calls are recorded dual-channel,
with the agent on the left channel and the other party on the right.

Refusals arrive as the usual exceptions with `ApiErrorCode` set: `402`
`InsufficientCreditsException` (`insufficient_credits`, the balance cannot cover
one minute), `428` `SendlyException` (`e911_required`, register an emergency
address), `409` `SendlyException` (`lines_busy`, retry shortly;
`agent_disabled`; `no_voice_number`), `400` `ValidationException`
(`agent_required`, `from_number_required`, `from_number_not_supported`: calls
can only be placed from US and Canadian numbers, `invalid_metadata`,
`destination_not_supported`), `404` `NotFoundException` (`agent_not_found`,
`number_not_found`, `call_not_found`, `outbound_calls_not_enabled`), `403`
`SendlyException` (`live_key_required`) and `429` `RateLimitException`
(`daily_call_limit`). Each is thrown on the first attempt, so a `409`
`lines_busy` is yours to retry. The constants live on `CallErrorCode`; statuses and hangup
reasons on `CallStatus` and `CallHangupClass`. `call.started`, `call.completed`
and `call.recording.ready` webhooks carry the same object in snake_case,
including `billing`, `channel` and your `metadata`. `Channel` is `phone`,
`whatsapp` or `browser` (constants on `CallChannel`; any other value comes
through unchanged). An inbound WhatsApp call can read `phone` until WhatsApp
calls are labelled on the inbound line.

### Configure voice

`client.Voice` configures everything a call depends on: which numbers take
calls and how they answer, each number's emergency address, and the AI agents
themselves. Reads need `calls:read` and writes `calls:write` with a live key.
In a team workspace, number and emergency-address writes also need a role that
can change settings, and agent writes a role that can manage API keys (each
agent holds its own scoped sending key); otherwise the API answers `403`
`forbidden`. A `number` is the number's id or its E.164 phone number.

Switching voice on for a number changes how real phone calls to it are
answered, and an agent answers real callers on every number pointed at it. A
US or Canadian number needs an emergency address before it can place calls;
the first registration adds $1.50 a month to the number, and registering again
replaces the address without charging twice.

```csharp
using Sendly.Resources;

// Numbers and how they answer
var numbers = await client.Voice.Numbers.ListAsync();
foreach (var n in numbers.Data)
    Console.WriteLine($"{n.PhoneNumber} {n.VoiceMode} {n.EmergencyAddress?.Status ?? "no emergency address"}");

var number = await client.Voice.Numbers.GetAsync("+15125550123");
Console.WriteLine($"{number.RatePerMinute.Outbound} credits a minute outbound");

// Register the emergency address (Country defaults to US)
number = await client.Voice.Numbers.RegisterEmergencyAddressAsync("+15125550123", new EmergencyAddress
{
    Street = "500 Example Ave",
    Unit = "Suite 2",
    City = "Austin",
    State = "TX",
    Zip = "78701",
});
Console.WriteLine(number.EmergencyAddress!.Status); // "provisioning", then "active"

// Voices, then an agent
var voices = await client.Voice.Voices.ListAsync();
var agent = await client.Voice.Agents.CreateAsync(new CreateVoiceAgentRequest
{
    Name = "Front desk",
    Voice = voices.Data[0].Id,
    Greeting = "Thanks for calling Acme, how can I help?",
    Instructions = "Answer questions about opening hours and take a message for anything else.",
    Tools = new VoiceAgentToolsInput { SendSms = true },
});

// Have the agent answer the number
number = await client.Voice.Numbers.UpdateAsync("+15125550123", new UpdateVoiceNumberRequest
{
    VoiceEnabled = true,
    VoiceMode = VoiceMode.Agent,
    AgentId = agent.Id,
});

// Change an agent (only the properties you set are sent)
agent = await client.Voice.Agents.UpdateAsync(agent.Id, new UpdateVoiceAgentRequest
{
    Greeting = "Thanks for calling Acme. How can I help today?",
});

// Ring the team instead, then delete the agent
await client.Voice.Numbers.UpdateAsync("+15125550123", new UpdateVoiceNumberRequest
{
    VoiceMode = VoiceMode.RingDashboard,
});
var deleted = await client.Voice.Agents.DeleteAsync(agent.Id);
Console.WriteLine(deleted.Deleted); // true
```

A mode alone is enough: `VoiceMode.RingDashboard` or `VoiceMode.Agent`
switches voice on, so it can fail the way switching on does (`502`
`voice_attach_failed`, `503` `voice_unavailable`), and `VoiceMode.None`
switches it off. `VoiceEnabled = false` wins over any mode, and
`VoiceMode.None` with `VoiceEnabled = true` becomes `ring_dashboard`. An empty
`AgentId` clears the stored agent, and an empty `TransferTo` clears that tool.
A workspace can hold up to 20 agents (`409` `agent_limit`). Agents cannot
transfer calls yet: while `TransferTo` is set, a caller who asks for a person
is told the message will be passed on, and the agent takes their name and
number.

Refusals carry `ApiErrorCode` as usual: `400` `ValidationException`
(`invalid_request`, `invalid_voice_mode`, `agent_required`, `invalid_address`,
`e911_not_applicable`), `404` `NotFoundException` (`number_not_found`,
`agent_not_found`), `409` `SendlyException` (`agent_disabled`, `agent_limit`,
`agent_in_use`), `422` `ValidationException` (`invalid_address`, the address
couldn't be validated), `502` `SendlyException` (`voice_attach_failed`,
`carrier_refused`) and `503` `SendlyException` (`voice_unavailable`). A `5xx`
is thrown only after the client has already retried it on its own. Not every
`carrier_refused` is worth retrying: when the message says the number couldn't
be found for emergency registration, retrying won't help, so contact support;
when it says the address couldn't be registered or emergency calling couldn't
be switched on, try again later. A 422 is thrown as a `ValidationException` like a 400,
so tell the two `invalid_address` refusals apart by the `suggested` field the
422 carries. Extra fields are on `ResponseBody`:

```csharp
try
{
    await client.Voice.Agents.DeleteAsync(agent.Id);
}
catch (SendlyException e) when (e.ApiErrorCode == CallErrorCode.AgentInUse)
{
    foreach (var n in e.ResponseBody!.Value.GetProperty("numbers").EnumerateArray())
        Console.WriteLine($"Still answering {n.GetString()}");
}
catch (ValidationException e) when (e.ApiErrorCode == CallErrorCode.InvalidAddress)
{
    if (e.ResponseBody is { } body && body.TryGetProperty("suggested", out var suggested))
        Console.WriteLine($"Did you mean: {suggested}"); // 422: null when no correction was found
    else
        Console.WriteLine(e.Message);                    // 400: a field is missing or malformed
}
```

## Verify (OTP)

One-time passcodes, with optional reusable templates and a hosted verification
page. On a test key the response is a sandbox verification and carries the code
to use in `SandboxCode`.

```csharp
using Sendly.Models;

var sent = await client.Verify.SendAsync(new SendVerificationRequest
{
    To = "+12025550143",
    AppName = "Acme",
    CodeLength = 6,
    TimeoutSecs = 600,
});
Console.WriteLine(sent.Id);
Console.WriteLine(sent.Sandbox);      // true on a test key
Console.WriteLine(sent.SandboxCode);  // the code to use in sandbox

// Check a code. A wrong code throws ValidationException (invalid_code) with the
// attempts left in its ResponseBody; a check that returns is always verified.
try
{
    var check = await client.Verify.CheckAsync(sent.Id, "123456");
    Console.WriteLine(check.IsVerified); // true
}
catch (Sendly.Exceptions.ValidationException e) when (e.ApiErrorCode == "invalid_code")
{
    if (e.ResponseBody is { } body && body.TryGetProperty("remaining_attempts", out var left))
        Console.WriteLine($"{left} attempts left");
}
catch (Sendly.Exceptions.RateLimitException e) when (e.ApiErrorCode == "max_attempts_exceeded")
{
    Console.WriteLine("No attempts left: send a new code"); // thrown at once, never retried
}

// Resend, read, list
await client.Verify.ResendAsync(sent.Id);
var verification = await client.Verify.GetAsync(sent.Id);
Console.WriteLine($"{verification.Status} {verification.Attempts}/{verification.MaxAttempts}");

var list = await client.Verify.ListAsync(new ListVerificationsOptions { Limit = 20 });
foreach (var v in list.Verifications.Where(x => x.Status == "verified"))
    Console.WriteLine($"{v.Phone}: {v.Status}");
```

### Hosted verification sessions

Send the user to a Sendly-hosted page instead of collecting the code yourself,
then validate the token it hands back.

```csharp
var session = await client.Verify.Sessions.CreateAsync(new CreateSessionRequest
{
    SuccessUrl = "https://acme.example/verified",
    CancelUrl = "https://acme.example/cancelled",
    BrandName = "Acme",
    BrandColor = "#0B6E4F",
});
Console.WriteLine(session.Url); // send the user here

var result = await client.Verify.Sessions.ValidateAsync(new ValidateSessionRequest
{
    Token = tokenFromSuccessUrl,
});
Console.WriteLine($"{result.Valid}: {result.Phone} at {result.VerifiedAt}");
```

## Contacts & Lists

```csharp
var contacts = await client.Contacts.ListAsync(new ListContactsOptions { Limit = 50, Search = "sam" });
foreach (var c in contacts.Contacts)
    Console.WriteLine($"{c.PhoneNumber} {c.Name} (opted out: {c.OptedOut})");

var contact = await client.Contacts.CreateAsync(new CreateContactRequest
{
    PhoneNumber = "+12025550143",
    Name = "Sam Reyes",
});
// Only the properties you set are sent: this changes the email and keeps the
// name and metadata. Set a property to null to clear it.
await client.Contacts.UpdateAsync(contact.Id, new UpdateContactRequest { Email = "sam@acme.example" });
await client.Contacts.UpdateAsync(contact.Id, new UpdateContactRequest { Metadata = null });
var one = await client.Contacts.GetAsync(contact.Id);
await client.Contacts.DeleteAsync(contact.Id);

// Import in bulk, into a list, with one opt-in date for rows that carry none
var imported = await client.Contacts.ImportAsync(new ImportContactsRequest
{
    Contacts = new() { new ImportContactItem { Phone = "+12025550143", Name = "Sam" } },
    ListId = "lst_xxx",
    OptedInAt = "2026-09-01T00:00:00Z",
});
Console.WriteLine($"{imported.Imported} imported, {imported.SkippedDuplicates} duplicates, {imported.TotalErrors} errors");
foreach (var error in imported.Errors) // the first 50
    Console.WriteLine($"Row {error.Index} ({error.Phone}) failed");
```

`ListId` adds every imported contact to the list, including those that
already existed.

### List health

Contacts are auto-flagged when a send fails with a terminal bad-number error or
a carrier lookup reports the number can't receive SMS. Clear the flag one at a
time, or in bulk when auto-flag misclassifies at scale.

```csharp
await client.Contacts.MarkValidAsync(contact.Id);

var cleared = await client.Contacts.BulkMarkValidAsync(new BulkMarkValidRequest
{
    ListId = "lst_xxx",   // or Ids (up to 10,000), never both
});
Console.WriteLine($"{cleared.Cleared} cleared");

// Background carrier lookup (1-5 minutes; re-triggering is a no-op)
var lookup = await client.Contacts.CheckNumbersAsync(new CheckNumbersRequest { ListId = "lst_xxx" });
Console.WriteLine(lookup.AlreadyRunning);
```

### Contact lists

```csharp
var lists = await client.Contacts.Lists.ListAsync();
foreach (var l in lists.Lists)
    Console.WriteLine($"{l.Name}: {l.ContactCount} contacts");

var list = await client.Contacts.Lists.CreateAsync(new CreateContactListRequest { Name = "VIPs" });
await client.Contacts.Lists.UpdateAsync(list.Id, new UpdateContactListRequest { Description = "Top spenders" });
await client.Contacts.Lists.AddContactsAsync(list.Id, new List<string> { contact.Id });
await client.Contacts.Lists.RemoveContactAsync(list.Id, contact.Id);
await client.Contacts.Lists.DeleteAsync(list.Id);
```

## Campaigns

```csharp
var campaign = await client.Campaigns.CreateAsync(new CreateCampaignRequest
{
    Name = "Spring sale",
    Text = "Hi {{name}}, 20% off this weekend only!",
    ContactListIds = new() { "lst_xxx" },
});

// Dry run before spending credits
var preview = await client.Campaigns.PreviewAsync(campaign.Id);
Console.WriteLine($"{preview.SendableCount} of {preview.RecipientCount} sendable " +
    $"({preview.OptedOutCount} opted out, {preview.InvalidCount} invalid)");
Console.WriteLine($"{preview.EstimatedCredits} credits, balance {preview.CurrentBalance}, enough: {preview.HasEnoughCredits}");

// Edit it until it is sent (a draft or a scheduled campaign)
await client.Campaigns.UpdateAsync(campaign.Id, new UpdateCampaignRequest { Name = "Spring sale v2" });

// Send now. The API answers with the message batch the send created, so the
// campaign returned carries Id, BatchId and the batch's counts, and its Status
// is the batch's status; call GetAsync for the campaign's name, text and dates.
var sent = await client.Campaigns.SendAsync(campaign.Id);
Console.WriteLine($"{sent.BatchId}: {sent.SentCount}/{sent.RecipientCount} sent, {sent.CreditsUsed} credits");
var batch = await client.Messages.GetBatchAsync(sent.BatchId!);

// Or schedule one (a draft only), and cancel the schedule (it becomes cancelled)
var later = await client.Campaigns.CloneAsync(campaign.Id);
await client.Campaigns.ScheduleAsync(later.Id, new ScheduleCampaignRequest
{
    ScheduledAt = DateTime.UtcNow.AddDays(1).ToString("s") + "Z",
    Timezone = "America/Chicago",
});
await client.Campaigns.CancelAsync(later.Id);

var campaigns = await client.Campaigns.ListAsync(new ListCampaignsOptions { Status = CampaignStatus.Completed });
foreach (var c in campaigns.Campaigns)
    Console.WriteLine($"{c.Name}: {c.SentCount}/{c.RecipientCount} sent, {c.DeliveredCount} delivered");

await client.Campaigns.DeleteAsync(later.Id);
```

The API reports a campaign's status as `draft`, `scheduled`, `sending`,
`completed` (sent), `cancelled` or `failed`, and `CampaignStatus` has a
constant for each. `CampaignStatus.Sent` and `Paused` are never returned;
`Sent` as a list filter matches completed campaigns.

## Conversations

Threaded inbound/outbound history per phone number.

```csharp
var conversations = await client.Conversations.ListAsync(new ListConversationsOptions
{
    Status = Conversation.Statuses.Active,
    Limit = 25,
});
foreach (var c in conversations.Data)
    Console.WriteLine($"{c.PhoneNumber} ({c.Channel}): {c.UnreadCount} unread, last {c.LastMessageAt}");
// A group MMS thread has IsGroup true and the other numbers in Participants

// One conversation, with its messages
var thread = await client.Conversations.GetAsync("cnv_xxx", new GetConversationOptions
{
    IncludeMessages = true,
    MessageLimit = 50,
});
foreach (var m in thread.Messages?.Data ?? new())
    Console.WriteLine($"{m.Direction}: {m.Text}");

// Reply, then manage the thread
await client.Conversations.ReplyAsync("cnv_xxx", new ReplyToConversationRequest { Text = "On our way!" });
await client.Conversations.MarkReadAsync("cnv_xxx");
await client.Conversations.CloseAsync("cnv_xxx");
await client.Conversations.ReopenAsync("cnv_xxx");
await client.Conversations.UpdateAsync("cnv_xxx", new UpdateConversationRequest
{
    Tags = new() { "vip" },
});

// Labels on a conversation
await client.Conversations.AddLabelsAsync("cnv_xxx", new AddLabelsRequest { LabelIds = new() { "lbl_xxx" } });
await client.Conversations.RemoveLabelAsync("cnv_xxx", "lbl_xxx");
```

### AI helpers

```csharp
// A compact, token-budgeted summary for your own LLM calls
var context = await client.Conversations.GetContextAsync("cnv_xxx", maxMessages: 20);
Console.WriteLine($"{context.TokenEstimate} tokens: {context.Context}");

// Suggested replies
var suggestions = await client.Conversations.SuggestRepliesAsync("cnv_xxx");
foreach (var s in suggestions.Suggestions)
    Console.WriteLine($"[{s.Tone}] {s.Text}");
```

## Labels

```csharp
var label = await client.Labels.CreateAsync(new CreateLabelRequest
{
    Name = "Escalated",
    Color = "#B3261E",
});

var labels = await client.Labels.ListAsync();
foreach (var l in labels.Data)
    Console.WriteLine($"{l.Name} {l.Color}");

await client.Labels.DeleteAsync(label.Id);
```

## Drafts

Queue a reply for a human to approve before it sends.

```csharp
var draft = await client.Drafts.CreateAsync(new CreateDraftRequest
{
    ConversationId = "cnv_xxx",
    Text = "We can refund that today — want me to start it?",
    Source = "ai",
});

var drafts = await client.Drafts.ListAsync(new ListDraftsOptions { Status = Draft.Statuses.Pending });
foreach (var d in drafts.Data)
    Console.WriteLine($"{d.Id}: {d.Text}");

await client.Drafts.UpdateAsync(draft.Id, new UpdateDraftRequest { Text = "We can refund that today." });
await client.Drafts.ApproveAsync(draft.Id);           // sends it
await client.Drafts.RejectAsync(draft.Id, "Off-tone"); // or reject with a reason
var read = await client.Drafts.GetAsync(draft.Id);
Console.WriteLine(read.MessageId); // set once an approved draft has sent
```

Draft statuses are `pending`, `approved`, `rejected`, `sent` and `failed`.

## Rules

Automations that label inbound messages by their AI classification.

```csharp
var rule = await client.Rules.CreateAsync(new CreateRuleRequest
{
    Name = "Escalate angry complaints",
    Conditions = new()
    {
        new() { ["intent"] = "complaint", ["sentiment"] = "negative", ["intentConfidenceMin"] = 0.8 }
    },
    Actions = new()
    {
        new() { ["addLabels"] = new[] { "lbl_xxx" }, ["closeConversation"] = false }
    },
    Priority = 10,
});

var rules = await client.Rules.ListAsync();
foreach (var r in rules.Data)
    Console.WriteLine($"{r.Priority}: {r.Name} (enabled: {r.Enabled})");

// Only the properties you set are sent
await client.Rules.UpdateAsync(rule.Id, new UpdateRuleRequest { Priority = 1 });
await client.Rules.UpdateAsync(rule.Id, new UpdateRuleRequest { Enabled = false }); // switch it off
await client.Rules.DeleteAsync(rule.Id);
```

The API stores `conditions` and `actions` as one object each, and the SDK
sends each list as one object, with the dictionaries merged (a key in a later
dictionary replaces the same key in an earlier one); a rule reads back as a
one-element list. Condition keys are `intent` and `sentiment` (a string or a
list of strings), matched against the AI classification of each inbound
message, and `intentConfidenceMin` and `sentimentConfidenceMin` (0 to 1).
Intents are `question`, `appointment`, `complaint`, `order_status`,
`feedback`, `opt_out`, `greeting`, `confirmation` and `other`; sentiments are
`positive`, `neutral` and `negative`. Action keys are `addLabels` (label ids)
and `closeConversation`. Every enabled rule that matches applies, in
ascending `Priority` order, and only to messages that were classified, which
needs the `ai_classification` feature.

## Media

Upload a file and use the returned URL as an MMS attachment.

```csharp
var media = await client.Media.UploadAsync("/path/to/receipt.jpg");
Console.WriteLine($"{media.Url} ({media.ContentType}, {media.SizeBytes} bytes)");

// Or from a stream
using var stream = File.OpenRead("/path/to/receipt.jpg");
var uploaded = await client.Media.UploadAsync(stream, "receipt.jpg", "image/jpeg");

await client.Messages.SendAsync(new SendMessageRequest("+12025550143", "Your receipt")
{
    MediaUrls = new List<string> { media.Url },
});
```

The content type is inferred from the file extension when you pass a path, but
the API accepts only JPEG, PNG and GIF images of up to 600 KB. Any other type,
such as `.webp`, `.mp4`, `.mp3` or `.pdf`, and a larger file are refused with a
`500`, which the client retries and then throws as a `SendlyException`; a file
whose content is not really JPEG, PNG or GIF gets a `400`
(`ValidationException`). Uploads need the `sms:send` scope, and answer `403`
`feature_disabled` while MMS is not enabled for your account.

## Webhooks

```csharp
// Create a webhook endpoint
var created = await client.Webhooks.CreateAsync(new CreateWebhookOptions
{
    Url = "https://acme.example/webhooks/sendly",
    Events = new List<string> { Webhook.EventTypes.MessageDelivered, Webhook.EventTypes.MessageFailed },
    Mode = Webhook.Modes.Live
});

// CreateAsync returns a WebhookCreatedResponse: the webhook is nested, and the
// signing secret (whsec_...) sits beside it and is shown only here.
Console.WriteLine(created.Webhook.Id);   // "whk_..."
Console.WriteLine(created.Secret);       // Store securely!

// List all webhooks
var webhooks = await client.Webhooks.ListAsync();
foreach (var w in webhooks)
    Console.WriteLine($"{w.Id} {w.Url} healthy={w.IsHealthy}");

// Get a specific webhook
var wh = await client.Webhooks.GetAsync("whk_xxx");

// Update a webhook
await client.Webhooks.UpdateAsync("whk_xxx", new UpdateWebhookOptions
{
    Url = "https://hooks.acme.example/sendly",
    Events = new List<string> { "message.delivered", "message.failed", "message.sent" },
    IsActive = true
});

// Test a webhook. When your endpoint does not accept the test delivery, the
// API answers 400 and this throws a ValidationException with its message.
try
{
    var result = await client.Webhooks.TestAsync("whk_xxx");
    Console.WriteLine($"{result.Message}: HTTP {result.StatusCode} in {result.ResponseTimeMs}ms");
}
catch (ValidationException e)
{
    Console.WriteLine($"Test failed: {e.Message}");
}

// Rotate webhook secret
var rotation = await client.Webhooks.RotateSecretAsync("whk_xxx");
Console.WriteLine(rotation.Secret);

// Delete a webhook
await client.Webhooks.DeleteAsync("whk_xxx");

// List available webhook event types
var eventTypes = await client.Webhooks.ListEventTypesAsync();
foreach (var eventType in eventTypes)
{
    Console.WriteLine($"Event: {eventType}");
}
```

### Deliveries and recovery

Each attempt is recorded, and repeated failures open a circuit breaker that
pauses delivery. After five minutes one delivery goes through as a probe, and a
success closes the breaker again; `ResetCircuitAsync` closes it at once.

```csharp
var deliveries = await client.Webhooks.ListDeliveriesAsync("whk_xxx", new ListDeliveriesOptions { Limit = 50 });
foreach (var d in deliveries)
    Console.WriteLine($"{d.EventType} -> {d.HttpStatus} (attempt {d.AttemptNumber})");

var delivery = await client.Webhooks.GetDeliveryAsync("whk_xxx", "del_xxx");
await client.Webhooks.RetryDeliveryAsync("whk_xxx", "del_xxx");

// After an outage: reset the breaker first, then replay
await client.Webhooks.ResetCircuitAsync("whk_xxx");

// Re-fire deliveries we recorded but couldn't deliver
await client.Webhooks.RedeliverAsync("whk_xxx", new RedeliverOptions
{
    Since = DateTime.UtcNow.AddDays(-2).ToString("s") + "Z",
    Statuses = new List<string> { "failed", "cancelled" },
    Limit = 1000,
});

// Synthesize events that never got an audit row at all
await client.Webhooks.BackfillAsync("whk_xxx", new BackfillOptions
{
    EventTypes = new List<string> { "message.delivered" },
});
```

`RedeliverAsync` and `BackfillAsync` return the raw `JsonDocument` the API
answered with. Both reject with `409` while the circuit is open, so call
`ResetCircuitAsync` first. A redelivery keeps the original event id, and a
backfilled event carries the event id the original dispatch used, so dedupe on
`event.id`. Do not dedupe on `data.object.id`: a message's sent and delivered
events share it.

### Receiving events

`Sendly.Webhooks` (the static class, not the `client.Webhooks` resource)
verifies the signature and parses the body. Both helpers are static and take
the payload first: `Webhooks.VerifySignature(payload, signature, secret,
timestamp)` and `Webhooks.ParseEvent(payload, signature, secret, timestamp)`,
where `timestamp` is optional and skips the replay check when omitted. Hand
them the raw request body: the signature covers the exact bytes that were sent,
so a re-serialized object no longer matches.

Signatures are `sha256=<hex>` HMAC-SHA256 over `timestamp.payload` (or the bare
payload when no timestamp is given), and a timestamp more than five minutes
from now is rejected. `Webhooks.GenerateSignature(payload, secret, timestamp)`
produces the same value for tests.

`WebhookEvent.Data` is a *message* view of `data.object`. That is right for
`message.*` and wrong for everything else: `rcs_brand.*`, `rcs_agent.*`,
`whatsapp_account.*`, `whatsapp_template.*`, `call.*`, `short_code.*`,
`brand.*`, `campaign.*`, `assignment.*`, `number.*`, `port*`, `conversation.*`,
`draft.*`, `contact.*`, `contacts.*` and `verification.*` carry a different
object entirely, and nothing is raised when one arrives. `Data` comes back at
its defaults, except where a key name happens to coincide (`id`, `status`),
which is worse: it binds another record's value. Two accessors give you the
real payload, and both are populated for every event type, `message.*`
included:

- `ObjectAs<T>()` deserializes `data.object` into a type you name.
- `RawObject` is `data.object` as a `JsonElement`, exactly as it arrived.

`ObjectAs<T>()` uses the `System.Text.Json` defaults, which do not map
snake_case, so name each wire field with `[JsonPropertyName]` (or pass your own
`JsonSerializerOptions`). It deserializes, it does not validate: a field the
payload never carried comes back at its default.

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Sendly;

app.MapPost("/webhooks/sendly", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var payload = await reader.ReadToEndAsync(); // raw body, not a re-serialized object

    WebhookEvent evt;
    try
    {
        evt = Webhooks.ParseEvent(
            payload,
            request.Headers["X-Sendly-Signature"].ToString(),
            webhookSecret,
            request.Headers["X-Sendly-Timestamp"].ToString()
        );
    }
    catch (WebhookSignatureException)
    {
        return Results.Unauthorized();
    }

    switch (evt.Type)
    {
        case "message.delivered":
            // message.* events only: Data is the message
            Console.WriteLine($"{evt.Data.Id} delivered to {evt.Data.To}");
            break;

        case "rcs_agent.live":
        case "rcs_agent.rejected":
            var agent = evt.ObjectAs<RcsAgentEventObject>();
            Console.WriteLine($"Agent {agent?.AgentId} is {agent?.Stage} ({agent?.Reason})");
            break;

        case "verification.delivered":
            // WebhookVerificationData ships with the SDK and already matches
            var verification = evt.ObjectAs<WebhookVerificationData>();
            Console.WriteLine($"{verification?.Phone}: {verification?.DeliveryStatus}");
            break;

        case "contact.auto_flagged":
            // data.object is a contact, so evt.Data.Id holds the CONTACT id
            if (evt.RawObject is JsonElement contact)
            {
                var contactId = contact.GetProperty("id").GetString();
                var reason = contact.TryGetProperty("invalid_reason", out var r)
                    ? r.GetString()
                    : null;
                Console.WriteLine($"Contact {contactId} flagged: {reason}");
            }
            break;
    }

    return Results.Ok();
});

// data.object for rcs_agent.* is not message-shaped, so declare its fields
public class RcsAgentEventObject
{
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("stage")]
    public string? Stage { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
```

One trap worth naming: `contact.auto_flagged` carries a contact, so
`evt.Data.Id` holds the *contact* id rather than a message id. A handler that
keys on it acts on the wrong record. The `Webhook.EventTypes` constants in
`Sendly.Models` name every event the API emits and are the safest thing to put
in a subscription's `Events` list.

## Account & Credits

```csharp
// Get account information: the user, the workspace and the key you called with
var account = await client.Account.GetAsync();
Console.WriteLine($"{account.Email} in {account.Organization?.Name}");
Console.WriteLine($"Key {account.ApiKey?.Name} ({account.ApiKey?.Type})");
Console.WriteLine($"Verified: {account.Verification.IsFullyVerified} ({account.Verification.Status})");
Console.WriteLine($"{account.Limits.MessagesPerDay} messages a day"); // 100 on a test key, 10,000 on a live key

// Check credit balance
var credits = await client.Account.GetCreditsAsync();
Console.WriteLine($"Total: {credits.Balance} credits, {credits.AvailableBalance} available");
Console.WriteLine($"Reserved: {credits.ReservedBalance}");
Console.WriteLine(credits.BillingMode); // "prepaid", or "pooled" on an enterprise credit pool

// View credit transaction history
var transactions = await client.Account.ListTransactionsAsync();
foreach (var tx in transactions)
{
    Console.WriteLine($"{tx.Type}: {tx.Amount} credits - {tx.Description}");
}

// Move credits to another workspace you control
var transfer = await client.Account.TransferCreditsAsync("org_xxx", 500);
Console.WriteLine($"{transfer.SourceBalance} -> {transfer.TargetBalance}");

// List API keys
var keys = await client.Account.ListApiKeysAsync();
foreach (var key in keys)
{
    Console.WriteLine($"{key.Name} ({key.Type}): {key.Prefix} active={key.IsActive} scopes={string.Join(",", key.Scopes ?? new())}");
}

// Get a specific API key
var singleKey = await client.Account.GetApiKeyAsync("key_xxx");

// Get API key usage stats (the API counts the key's most recent 100 requests)
var usage = await client.Account.GetApiKeyUsageAsync("key_xxx");
Console.WriteLine($"Requests: {usage.TotalRequests}, last at {usage.LastRequestAt}");
Console.WriteLine($"Credits used: {usage.CreditsUsed}");
foreach (var request in usage.RecentRequests) // the last 20
    Console.WriteLine($"{request.Method} {request.Endpoint} -> {request.StatusCode}");
foreach (var endpoint in usage.EndpointBreakdown)
    Console.WriteLine($"{endpoint.Endpoint}: {endpoint.Count}");

// Create a new API key: a test key unless Type is "live". Scopes defaults to
// the calling key's scopes, and you can grant only scopes the calling key holds.
var newKey = await client.Account.CreateApiKeyAsync(new CreateApiKeyOptions
{
    Name = "Production Key",
    Type = "live",
    Scopes = new() { "sms:send", "sms:read" },
});
Console.WriteLine($"New key: {newKey.Key}"); // Only shown once!
Console.WriteLine(newKey.ApiKey.Id);

// Revoke an API key (the key you are authenticating with cannot be revoked)
await client.Account.RevokeApiKeyAsync("key_xxx");

// Rotate a key. Mints a replacement and keeps the old one working for a grace
// period (24-168 hours, default 24) so you can roll the new key out first.
var rotation = await client.Account.RotateApiKeyAsync("key_xxx", gracePeriodHours: 48);
Console.WriteLine(rotation.NewKey.Key);       // full raw sk_ value, shown once!
Console.WriteLine(rotation.NewKey.Warning);
Console.WriteLine(rotation.NewKey.Type);      // "test" or "live"
Console.WriteLine(rotation.OldKey.ExpiresAt); // when the old key stops working
Console.WriteLine(rotation.Message);
```

Scheduled messages are charged when you schedule them, so they come off
`Balance`, not `ReservedBalance`.

A live key needs a verified business and a credit balance: without them
`CreateApiKeyAsync` throws at once, a `SendlyException` with `StatusCode` 403
and `ApiErrorCode` `verification_required`, or an
`InsufficientCreditsException` (`credits_required`). `CreateApiKeyAsync(name)`
creates a test key.

`ApiKey` (from `ListApiKeysAsync` / `GetApiKeyAsync`) carries `Id`, `Name`,
`Prefix`, `Type`, `Scopes`, `IsActive`, `IsRevoked`, `IsExpired`,
`LastUsedAt`, `CreatedAt` and `ExpiresAt`. The API does not report
`ApiKeyUsage.SuccessfulRequests` or `FailedRequests` (both read 0), or
`Credits.PendingCredits`; count failures from `RecentRequests` instead.

## Error Handling

```csharp
using Sendly.Exceptions;

try
{
    var message = await client.Messages.SendAsync("+12025550143", "Hello!");
}
catch (AuthenticationException e)
{
    // Invalid API key (401)
}
catch (RateLimitException e) when (e.ApiErrorCode == "too_many_failed_key_attempts")
{
    // Too many wrong API keys from this address (429): fix the key, do not retry
}
catch (RateLimitException e)
{
    // A 429 the client did not wait out (see Rate Limits)
    Console.WriteLine($"{e.ApiErrorCode}: retry after {e.RetryAfter?.TotalSeconds} seconds");
}
catch (InsufficientCreditsException e)
{
    // Add more credits (402)
}
catch (ValidationException e)
{
    // Invalid request (StatusCode is 400 or 422)
    foreach (var fieldError in e.FieldErrors)
        Console.WriteLine(fieldError); // "brand.ein: Enter a 9-digit EIN"
}
catch (NotFoundException e)
{
    // Resource not found (404)
}
catch (NetworkException e)
{
    // Network error or timeout
}
catch (SendlyException e)
{
    // Other error
    Console.WriteLine(e.Message);
    Console.WriteLine(e.ErrorCode);
    Console.WriteLine(e.StatusCode);
}
```

Status codes map to types as follows: `401` → `AuthenticationException`, `402` →
`InsufficientCreditsException`, `404` → `NotFoundException`, `429` →
`RateLimitException`, `400` and `422` → `ValidationException`, and everything
else (including `403`, `409` and `5xx`) → `SendlyException` with `StatusCode`
set. `NetworkException` covers transport failures and timeouts. Any `4xx`
except a `408` and the `429`s listed under [Rate Limits](#rate-limits) is
thrown on the first attempt; a `5xx`, a timeout or a network failure only
after the client's own retries (see [Configuration](#configuration)).

Every exception also carries the API's own `error` string as `ApiErrorCode`
(for example `rcs_field_locked` and `rcs_launch_not_ready`, both 409s) and,
when the response lists per-field problems, `FieldErrors` (`Path` + `Message`
pairs such as `brand.ein: Enter a 9-digit EIN`). `ErrorCode` is unchanged and
still holds the per-class constant. `ResponseBody` is the whole JSON object the
API answered with (null when the error did not come from one), for refusals
that carry more than a message, such as the `numbers` on a 409 `agent_in_use`.

## Message Object

```csharp
message.Id           // Unique identifier
message.To           // Recipient phone number
message.From         // Sender number or ID (nullable)
message.Text         // Message content
message.Status       // queued, sent, delivered, read, failed, bounced, retrying
message.Direction    // outbound or inbound
message.Segments     // int
message.CreditsUsed  // Credits consumed
message.IsSandbox    // bool, on GetAsync and ListAsync
message.Simulated    // bool? true when a send was simulated, not delivered (send responses)
message.SimulatedReason // string? why a live-key send was simulated
message.SenderType   // number_pool, alphanumeric or explicit (live sends; nullable)
message.SenderNote   // string? a note on the sender a live send used
message.CreatedAt    // DateTime
message.DeliveredAt  // DateTime? (nullable)
message.ErrorCode    // string? (nullable)
message.ErrorMessage // string? (nullable)
message.RetryCount   // int
message.Metadata     // Dictionary<string, object>? (nullable)
message.AiMetadata   // AiMetadata? — intent/sentiment on inbound messages

// Helper properties
message.IsDelivered  // bool
message.IsFailed     // bool
message.IsPending    // bool (queued or sent)
```

The status constants live on `Message.Statuses`, directions on
`Message.Directions`, and sender types on `Message.SenderTypes`.
`SenderTypes.User`, `Api`, `System` and `Campaign` are `[Obsolete]`: the API
never sends those values. `UpdatedAt` is not sent by the API and keeps its
default.

## Message Status

| Status | Description |
|--------|-------------|
| `queued` | Message is queued for delivery |
| `sent` | Message was sent to carrier |
| `delivered` | Message was delivered |
| `read` | Recipient read it (RCS and WhatsApp only; SMS never reports one) |
| `failed` | Message delivery failed |
| `bounced` | Carrier rejected the message |
| `retrying` | Delivery is being retried |

## Pricing Tiers

1 credit is $0.01, so the credit column is the per-segment price in cents.

| Tier | Example countries | Credits per SMS |
|------|-------------------|-----------------|
| Domestic | US, CA | 2 |
| Tier 1 | GB, PL, AU | 8 |
| Tier 2 | FR, JP, IT, IN | 12 |
| Tier 3 | DE, NL, MX | 16 |
| Tier 4 | UA, VN, PA | 24 |
| Tier 5 | MY | 48 |

Enterprise accounts can hold per-country or per-tier credit overrides, so the
authoritative number for a specific send is the batch total `PreviewBatchAsync`
returns in `CreditsNeeded` (the API sends no per-message items, so
`BatchPreviewResponse.Messages` stays empty).

## Sandbox Testing

Use test API keys (`sk_test_v1_xxx`) with these test numbers:

| Number | Behavior |
|--------|----------|
| +15005550000 | Success (instant) |
| +15005550001 | Fails: invalid phone number |
| +15005550002 | Fails: cannot route to destination |
| +15005550003 | Fails: queue full |
| +15005550004 | Fails: rate limit exceeded |
| +15005550006 | Fails: carrier violation |

## Not in this SDK

These parts of the platform have no helper on `SendlyClient`. Call the REST API
directly if you need them; the notes below say what the SDK *does* carry.

- **Short codes.** No resource. Subscribing to the lifecycle events works
  (`Webhook.EventTypes.ShortCodeActionRequired`, `ShortCodeRejected`,
  `ShortCodeFiled`, `ShortCodeLive`), but leasing, filing and provisioning are
  dashboard- or REST-only — see [Short Codes](#short-codes).
- **Porting.** No resource. Again, the event constants exist
  (`PortCompleted`, `PortOutRequested`, `PortOutCompleted`, `PortOutRejected`,
  `PortOutCancelled`).
- **Teams and members.** No workspace-level member management. Enterprise
  parents can invite users into a workspace with
  `client.Enterprise.Workspaces.SendInvitationAsync` / `ListInvitationsAsync` /
  `CancelInvitationAsync`.

AI receptionists are not separate from voice: they are the agents you create
with [`client.Voice.Agents`](#configure-voice).

## Enterprise

The Enterprise API lets you programmatically manage workspaces, verification, credits, and API keys for multi-tenant platforms. It requires an enterprise master key — an ordinary live key flagged as your account's master and scoped `enterprise:master`, created once from the enterprise dashboard.

### Quick Provision

Create a fully configured workspace in a single call:

```csharp
var client = new SendlyClient("sk_live_v1_YOUR_MASTER_KEY");

var result = await client.Enterprise.ProvisionAsync(new ProvisionWorkspaceOptions
{
    Name = "Acme Insurance - Austin",
    SourceWorkspaceId = "ws_verified",
    CreditAmount = 5000,
    CreditSourceWorkspaceId = "SOURCE_WORKSPACE_ID",
    KeyName = "Production",
    KeyType = "live",
    GenerateOptInPage = true
});

Console.WriteLine(result.Workspace.Id);
Console.WriteLine(result.Key?.Key);
Console.WriteLine(result.OptInPage?.Url ?? result.OptInPage?.Error); // Error is set when the page could not be generated
```

Three provisioning modes:

| Mode | Params | Description |
|------|--------|-------------|
| **Inherit** | `SourceWorkspaceId` | Shares toll-free number from verified workspace |
| **Inherit + New Number** | `SourceWorkspaceId` + `InheritWithNewNumber = true` | Copies business info, purchases new number |
| **Fresh** | `Verification = new SubmitVerificationOptions{...}` | Full business details, new number + carrier approval |

### Workspace Management

```csharp
var ws = await client.Enterprise.Workspaces.CreateAsync("Acme Insurance");
var list = await client.Enterprise.Workspaces.ListAsync();
var detail = await client.Enterprise.Workspaces.GetAsync("ws_xxx");
await client.Enterprise.Workspaces.SuspendAsync("ws_xxx");
await client.Enterprise.Workspaces.ResumeAsync("ws_xxx");
await client.Enterprise.Workspaces.DeleteAsync("ws_xxx");
```

### Verification

```csharp
// A first submission needs the full record. State is the full name ("Texas").
await client.Enterprise.Workspaces.SubmitVerificationAsync("ws_xxx", new VerificationSubmitInput
{
    BusinessName = "Acme Insurance LLC",
    Website = "https://acme.example",
    Address = new VerificationAddress { Street = "100 Main St", City = "Austin", State = "Texas", Zip = "78701" },
    Contact = new VerificationContact { FirstName = "Jane", LastName = "Doe", Email = "jane@acme.example", Phone = "+15125550123" },
    UseCase = "Insurance Services",
    UseCaseSummary = "Policy renewal reminders for existing Acme Insurance customers.",
    SampleMessages = "Acme Insurance: your policy renews on 3/15. Reply STOP to opt out.",
    OptInWorkflow = "Customers opt in by ticking an SMS consent box on the policy form at https://acme.example.",
});

// After a rejection or a changes request, resubmit only the fields you want to
// change; everything else is preserved from the existing record.
await client.Enterprise.Workspaces.ResubmitVerificationAsync("ws_xxx", new VerificationSubmitInput
{
    Website = "https://acme.example/about",
});
// Share another workspace's verification and toll-free number
await client.Enterprise.Workspaces.InheritVerificationAsync("ws_xxx", "ws_verified");

// Or copy its business details and order the workspace its own toll-free
// number. Ordering and submitting are best effort, so check TollFreeNumber,
// which is null when no number could be ordered.
var inherited = await client.Enterprise.Workspaces.InheritVerificationAsync("ws_yyy", new InheritVerificationOptions
{
    SourceWorkspaceId = "ws_verified",
    PurchaseNewNumber = true,
});
Console.WriteLine($"{inherited.Status} {inherited.TollFreeNumber ?? "no number ordered"}");

var status = await client.Enterprise.Workspaces.GetVerificationAsync("ws_xxx");

// Supporting document upload
await client.Enterprise.UploadVerificationDocumentAsync("/path/to/ein.pdf", workspaceId: "ws_xxx");
```

For sole proprietors leave `Brn`, `BrnType` and `BrnCountry` null — the server
strips them before forwarding to the carrier.

### Credits & API Keys

```csharp
await client.Enterprise.Workspaces.TransferCreditsAsync("ws_dest", "ws_source", 5000);
var wsCredits = await client.Enterprise.Workspaces.GetCreditsAsync("ws_xxx");

var key = await client.Enterprise.Workspaces.CreateKeyAsync("ws_xxx", new CreateWorkspaceKeyOptions
{
    Name = "Production",
    Type = "live"
});
Console.WriteLine(key.Key);

var keys = await client.Enterprise.Workspaces.ListKeysAsync("ws_xxx");
await client.Enterprise.Workspaces.RevokeKeyAsync("ws_xxx", "key_abc");

// Shared credit pool
var pool = await client.Enterprise.Credits.GetAsync();
```

### Webhooks & Analytics

```csharp
await client.Enterprise.Webhooks.SetAsync("https://acme.example/webhooks");
await client.Enterprise.Webhooks.TestAsync();
await client.Enterprise.Webhooks.RotateSecretAsync();

var overview = await client.Enterprise.Analytics.OverviewAsync();
Console.WriteLine($"{overview.DeliveryRatePercent}% delivered across {overview.TotalWorkspaces} workspaces"); // DeliveryRate is the same, rounded to an int
var messages = await client.Enterprise.Analytics.MessagesAsync(new EnterpriseAnalyticsOptions { Period = "30d" });
var delivery = await client.Enterprise.Analytics.DeliveryAsync();
var creditUse = await client.Enterprise.Analytics.CreditsAsync(new EnterpriseAnalyticsOptions { Period = "30d" });
```

### Bulk, quotas and hosted pages

```csharp
// Up to 100 workspaces per call (more is refused before anything is sent)
var bulk = await client.Enterprise.Workspaces.ProvisionBulkAsync(workspaces);

var quota = await client.Enterprise.Workspaces.GetQuotaAsync("ws_xxx");
await client.Enterprise.Workspaces.SetQuotaAsync("ws_xxx", quotaUpdate);

var pages = await client.Enterprise.Workspaces.ListOptInPagesAsync("ws_xxx");
await client.Enterprise.GenerateBusinessPageAsync(pageOptions);
```

Full enterprise docs: [sendly.live/docs/enterprise](https://sendly.live/docs/enterprise)

---

## License

MIT
