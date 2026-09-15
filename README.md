<p align="center">
  <img src="https://raw.githubusercontent.com/SendlyHQ/sendly-dotnet/main/.github/header.svg" alt="Sendly .NET SDK" />
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/Sendly"><img src="https://img.shields.io/nuget/v/Sendly.svg?style=flat-square" alt="NuGet" /></a>
  <a href="https://github.com/SendlyHQ/sendly-dotnet/blob/main/LICENSE"><img src="https://img.shields.io/github/license/SendlyHQ/sendly-dotnet?style=flat-square" alt="license" /></a>
</p>

# Sendly .NET SDK

Official .NET SDK for the Sendly SMS API.

## Requirements

- .NET 8.0+

## Installation

```bash
# .NET CLI
dotnet add package Sendly

# Package Manager Console
Install-Package Sendly

# PackageReference (add to .csproj)
<PackageReference Include="Sendly" Version="4.2.0" />
```

## Quick Start

```csharp
using Sendly;

using var client = new SendlyClient("sk_live_v1_your_api_key");

// Send an SMS
var message = await client.Messages.SendAsync(
    "+15551234567",
    "Hello from Sendly!"
);

Console.WriteLine(message.Id);     // "msg_abc123"
Console.WriteLine(message.Status); // "queued"
```

## Prerequisites for Live Messaging

Before sending live SMS messages, you need:

1. **Business Verification** - Complete verification in the [Sendly dashboard](https://sendly.live/dashboard)
   - **International**: Instant approval (just provide Sender ID)
   - **US/Canada**: Requires carrier approval (3-7 business days)

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
    MaxRetries = 5
});
```

## Messages

### Send an SMS

```csharp
// Marketing message (default)
var message = await client.Messages.SendAsync("+15551234567", "Check out our new features!");

// Transactional message (bypasses quiet hours)
var message = await client.Messages.SendAsync(new SendMessageRequest(
    "+15551234567",
    "Your verification code is: 123456"
) { MessageType = "transactional" });

// With custom metadata (max 4KB)
var message = await client.Messages.SendAsync(new SendMessageRequest(
    "+15551234567",
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
    "+15551234567",
    "Hello from our team!"
) { From = "+447111111111" });

Console.WriteLine(message.Id);
Console.WriteLine(message.Status);
Console.WriteLine(message.CreditsUsed);
```

### List Messages

```csharp
// Basic listing
var messages = await client.Messages.ListAsync();

foreach (var msg in messages)
{
    Console.WriteLine(msg.To);
}

// With options
var messages = await client.Messages.ListAsync(new ListMessagesOptions
{
    Status = "delivered",
    To = "+15551234567",
    Limit = 50,
    Offset = 0
});

// Pagination info
Console.WriteLine(messages.Total);
Console.WriteLine(messages.HasMore);
```

### Get a Message

```csharp
var message = await client.Messages.GetAsync("msg_abc123");

Console.WriteLine(message.To);
Console.WriteLine(message.Text);
Console.WriteLine(message.Status);
Console.WriteLine(message.DeliveredAt);
```

### Scheduling Messages

```csharp
// Schedule a message for future delivery
var scheduled = await client.Messages.ScheduleAsync(new ScheduleMessageRequest(
    "+15551234567",
    "Your appointment is tomorrow!",
    "2025-01-15T10:00:00Z"
));

Console.WriteLine(scheduled.Id);
Console.WriteLine(scheduled.ScheduledAt);

// List scheduled messages
var result = await client.Messages.ListScheduledAsync();
foreach (var msg in result)
{
    Console.WriteLine($"{msg.Id}: {msg.ScheduledAt}");
}

// Get a specific scheduled message
var msg = await client.Messages.GetScheduledAsync("sched_xxx");

// Cancel a scheduled message (refunds credits)
var cancel = await client.Messages.CancelScheduledAsync("sched_xxx");
Console.WriteLine($"Refunded: {cancel.CreditsRefunded} credits");
```

### Batch Messages

```csharp
// Send multiple messages in one API call (up to 1000)
var batch = await client.Messages.SendBatchAsync(new SendBatchRequest()
    .AddMessage("+15551234567", "Hello User 1!")
    .AddMessage("+15559876543", "Hello User 2!")
    .AddMessage("+15551112222", "Hello User 3!")
);

Console.WriteLine(batch.BatchId);
Console.WriteLine($"Sent: {batch.Sent}");
Console.WriteLine($"Failed: {batch.Failed}");
Console.WriteLine($"Credits used: {batch.CreditsUsed}");

// Get batch status. QueuedCount and CreatedAt are reported only here and by the
// list endpoint, so they are null on the send response above.
var status = await client.Messages.GetBatchAsync(batch.BatchId);
Console.WriteLine($"Queued: {status.QueuedCount}");

// List all batches
var batches = await client.Messages.ListBatchesAsync();

// Preview batch (dry run) - validates without sending
var preview = await client.Messages.PreviewBatchAsync(new SendBatchRequest()
    .AddMessage("+15551234567", "Hello User 1!")
    .AddMessage("+447700900123", "Hello UK!")
);
Console.WriteLine($"Credits needed: {preview.CreditsNeeded}");
Console.WriteLine($"Will send: {preview.WillSend}, Blocked: {preview.Blocked}");
```

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

### Group MMS

```csharp
// Send a group MMS to 2-8 US/Canada recipients. Everyone sees the others and
// replies fan out to the group. Omit From to use your default sender.
var group = await client.Messages.SendGroupAsync(new SendGroupMessageRequest(
    new[] { "+14155551234", "+14155555678" },
    "Hey team - quick sync at noon?"
));

Console.WriteLine(group.Id);              // "msg_abc123"
Console.WriteLine(group.GroupMessageId);  // "grp_xxx" (on live sends)
Console.WriteLine(group.Status);          // "sent" or "delivered"
```

### AI Message Enhancement

```csharp
// Rewrite a draft into a single polished SMS segment (<=160 chars).
var result = await client.Messages.EnhanceAsync(new EnhanceMessageRequest(
    text: "hey come check out our sale this weekend",
    messageType: "marketing"
));

Console.WriteLine(result.Enhanced);     // polished rewrite
Console.WriteLine(result.Explanation);  // what changed and why
```

## Idempotency

POSTs carry an automatically generated `Idempotency-Key`, reused across the
SDK's own timeout and network-error retries, so a retry of a request that
already reached the API returns the original result instead of sending and
charging again; after a `5xx` the generated key is rotated so the retry really
does run again. Pass your own key through the `IdempotentRequestOptions`
overloads of `SendAsync`, `SendGroupAsync`, `SendBatchAsync`, and
`ScheduleAsync` when the guarantee needs to outlive the process, such as a job
queue that re-runs after a crash. Reusing a key within 24 hours returns the
original response, and reusing it with a different body is rejected with a
`422`, so derive keys from something stable in your domain, like an order id.
`SendBatchAsync` sends no automatic key, because the API already deduplicates
identical batches by their contents. The RCS registration writes
(`client.Rcs.Brands` and `client.Rcs.Agents`) and the voice writes
(`client.Calls` and `client.Voice`) take an optional `IdempotentRequestOptions`
too; their `PATCH`, `PUT` and `DELETE` calls send a key only when you supply
one.

```csharp
var message = await client.Messages.SendAsync(
    new SendMessageRequest("+15551234567", "Your order has shipped!"),
    new IdempotentRequestOptions { IdempotencyKey = "order-4821-shipped" }
);
```

Full details: https://sendly.live/docs/idempotency

## Message Templates

Reusable SMS templates with `{{variables}}`, published for use with the Verify
API. (`client.Templates` is a second, slimmer view of the same `/templates`
endpoint.)

```csharp
// List presets + custom templates
var listing = await client.MessageTemplates.ListAsync();

// Create a draft, then publish
var template = await client.MessageTemplates.CreateAsync(new CreateMessageTemplateRequest
{
    Name = "Password Reset",
    Text = "{{app_name}}: your reset code is {{code}}. Valid for 10 minutes."
});
await client.MessageTemplates.PublishAsync(template.Id);

// Preview with sample values
var preview = await client.MessageTemplates.PreviewAsync(template.Id, new Dictionary<string, string>
{
    ["app_name"] = "MyApp",
    ["code"] = "123456"
});
Console.WriteLine(preview.PreviewText);

// Clone (including from a preset), update, delete
var clone = await client.MessageTemplates.CloneAsync("tpl_preset_otp", "My Custom OTP");
await client.MessageTemplates.DeleteAsync(clone.Id);
```

## Branded Short Links

Mint branded, owned-domain short links (better carrier deliverability than
public shorteners) with click analytics and a per-link kill switch.

> **Note:** URL shortening is gated behind the founder-only `url_shortener`
> flag and is not yet publicly stable. Calls raise `NotFoundException`
> (`not_found`) until the flag is on for your account.

```csharp
// Shorten a URL
var link = await client.Links.CreateAsync("https://example.com/spring-sale");
Console.WriteLine(link.ShortUrl); // "https://sendly.live/l/Ab3xY7"

// List your links with click counts
var links = await client.Links.ListAsync(new ListShortLinksOptions { Limit = 20 });
foreach (var l in links.Links)
{
    Console.WriteLine($"{l.ShortUrl} -> {l.DestinationUrl} ({l.ClickCount} clicks)");
}

// Kill / re-enable a link
await client.Links.DisableAsync(link.Code);
await client.Links.EnableAsync(link.Code);
```

## Numbers

```csharp
// Browse coverage and search for an available number
var countries = await client.Numbers.ListCountriesAsync();
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

## WhatsApp

Connect a number you own to WhatsApp ($19 one-time setup, no monthly fee),
create Meta-reviewed message templates, and send with
`client.Messages.SendAsync(new SendWhatsAppMessageRequest(...))`. Free-form
text and media only deliver inside an open 24-hour customer-service window
(the recipient messaged you in the last 24h); an approved template works
anytime. WhatsApp requires a live API key.

```csharp
// 1. Connect a number. The connect URL must be opened by a human — they log
//    in with Facebook in a browser to link their WhatsApp Business Account.
var signup = await client.WhatsApp.Signup.CreateAsync("+15559876543");
Console.WriteLine($"Have your user open: {signup.ConnectUrl}");

// 2. Poll until active
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
    Sender = "+15559876543",
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

// 4. Check the 24-hour window, then send
var window = await client.WhatsApp.WindowAsync("+15559876543", "+15551234567");

// Free-form text inside an open window
var message = await client.Messages.SendAsync(new SendWhatsAppMessageRequest(
    "+15551234567",
    "+15559876543",
    text: "Your table is ready!"
));

// Media with a caption (window-bound; WhatsApp accepts exactly one attachment)
await client.Messages.SendAsync(new SendWhatsAppMessageRequest(
    "+15551234567",
    "+15559876543",
    text: "Here's your receipt",
    mediaUrls: new() { "https://example.com/receipt.pdf" }
));

// Template send — works regardless of the window
await client.Messages.SendAsync(new SendWhatsAppMessageRequest(
    "+15551234567",
    "+15559876543",
    template: new WhatsAppTemplateSendParams
    {
        Name = "order_shipped",
        Language = "en_US",
        Variables = new() { ["1"] = "Sam", ["2"] = "#4821" }
    }
));

Console.WriteLine(message.WhatsApp.Kind); // "text", "media", or "template"
Console.WriteLine(message.CreditsUsed);   // priced by country + category

// 5. Read and edit a sender's business profile — the contact card recipients
//    see. Supply only the fields to change; omitted fields keep their value.
var profile = await client.WhatsApp.Senders.GetProfileAsync("+15559876543");
Console.WriteLine($"{profile.DisplayName} — {profile.About}");

await client.WhatsApp.Senders.UpdateProfileAsync("+15559876543",
    new UpdateWhatsAppSenderProfileRequest
    {
        About = "Fast delivery, friendly service",  // max 139 chars
        Description = "Acme sells everything.",     // max 512 chars
        Website = "https://example.com"
    });
```

## RCS

Send branded rich messaging — cards and suggestion chips — with
`client.Messages.SendAsync(new SendRcsMessageRequest(...))`. Plain-text RCS
sends fall back to SMS automatically for recipients whose device can't receive
RCS. RCS is gated behind the `rcs_channel` rollout flag (default-dark): while
it is off for your account every `client.Rcs` call throws `NotFoundException`
(`ApiErrorCode` `rcs_not_enabled`). Sends and capability checks require a live
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
var capability = await client.Rcs.CapabilityAsync("+15551234567");
Console.WriteLine(capability.Capable);  // false -> text falls back to SMS
Console.WriteLine(string.Join(", ", capability.Features));

// 3. Send text, optionally with suggestion chips. A reply chip's tap comes
//    back as an inbound message carrying your postbackData; an action chip
//    opens a URL.
var message = await client.Messages.SendAsync(new SendRcsMessageRequest(
    "+15551234567",
    text: "Your order #4821 has shipped!",
    suggestions: new()
    {
        RcsSuggestion.CreateReply("Thanks", "thanks"),
        RcsSuggestion.CreateAction("Track", "track", "https://example.com/track/4821")
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
    Console.WriteLine(message.Rcs.AgentName);  // "Acme Inc"
}

// 4. Send a rich card. Cards have no SMS form — a card to a non-RCS recipient
//    fails with rcs_not_supported_for_recipient rather than falling back.
await client.Messages.SendAsync(new SendRcsMessageRequest(
    "+15551234567",
    card: new RcsCard
    {
        Title = "Order #4821 shipped",
        Description = "Arriving Thursday",
        MediaUrl = "https://example.com/package.jpg",  // public JPEG, PNG, or GIF
        Orientation = "vertical",                      // or "horizontal"
        Suggestions = new()
        {
            RcsSuggestion.CreateAction("Track", "track", "https://example.com/track/4821")
        }
    }
));

// Opt out of the SMS fallback to get a 422 instead of an SMS charge
await client.Messages.SendAsync(new SendRcsMessageRequest(
    "+15551234567",
    text: "RCS only, please",
    fallbackToSms: false
));

// Pass agentId when your workspace has more than one agent (otherwise the
// send fails with rcs_agent_ambiguous)
await client.Messages.SendAsync(new SendRcsMessageRequest(
    "+15551234567",
    text: "Your order #4821 has shipped!",
    agentId: "rag_abc123"
));
```

## Voice Calls

Place phone calls handled by your AI agents, list and inspect calls, end a
call, and download recordings. Over the API a call is always answered by one
of your agents (created with `client.Voice.Agents` or in the dashboard under
Calls → Agents); the agent speaks first and follows any `Context` you attach.
Reads need an API key with the `calls:read` scope, writes `calls:write` and a
live key.

Calls are prepaid from your credit balance per started minute: an agent-handled
outbound call costs 10 credits a minute ($0.10), inbound calls 2 to 3 credits
plus 8 for the agent, and unanswered calls cost nothing. Destinations are US
and Canada. The number you call from must have voice enabled and an emergency
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
    To = "+15555550123",
    AgentId = "3c4d5e6f-7081-4293-a4b5-c6d7e8f90a1b",
    From = "+15555550188",                        // optional with one voice-enabled number
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

Refusals arrive as the usual exceptions with `ApiErrorCode` set: `402`
`InsufficientCreditsException` (`insufficient_credits`, the balance cannot cover
one minute), `428` `SendlyException` (`e911_required`, register an emergency
address), `409` `SendlyException` (`lines_busy`, retry shortly;
`agent_disabled`; `no_voice_number`), `400` `ValidationException`
(`agent_required`, `from_number_required`, `invalid_metadata`,
`destination_not_supported`), `404` `NotFoundException` (`agent_not_found`,
`number_not_found`, `call_not_found`), `403` `SendlyException`
(`live_key_required`) and `429` `RateLimitException` (`daily_call_limit`). The
constants live on `CallErrorCode`; statuses and hangup reasons on `CallStatus`
and `CallHangupClass`. `call.started`, `call.completed` and
`call.recording.ready` webhooks carry the same object in snake_case, including
`billing` and your `metadata`.

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

var number = await client.Voice.Numbers.GetAsync("+15555550188");
Console.WriteLine($"{number.RatePerMinute.Outbound} credits a minute outbound");

// Register the emergency address (Country defaults to US)
number = await client.Voice.Numbers.RegisterEmergencyAddressAsync("+15555550188", new EmergencyAddress
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
number = await client.Voice.Numbers.UpdateAsync("+15555550188", new UpdateVoiceNumberRequest
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
await client.Voice.Numbers.UpdateAsync("+15555550188", new UpdateVoiceNumberRequest
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
Agents cannot transfer calls yet: while `TransferTo` is set, a caller who asks
for a person is told the message will be passed on, and the agent takes their
name and number.

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

## Webhooks

```csharp
// Create a webhook endpoint
var webhook = await client.Webhooks.CreateAsync(new CreateWebhookOptions
{
    Url = "https://example.com/webhooks/sendly",
    Events = new List<string> { "message.delivered", "message.failed" }
});

Console.WriteLine(webhook.Id);
Console.WriteLine(webhook.Secret); // Store securely!

// List all webhooks
var webhooks = await client.Webhooks.ListAsync();

// Get a specific webhook
var wh = await client.Webhooks.GetAsync("whk_xxx");

// Update a webhook
await client.Webhooks.UpdateAsync("whk_xxx", new UpdateWebhookOptions
{
    Url = "https://new-endpoint.example.com/webhook",
    Events = new List<string> { "message.delivered", "message.failed", "message.sent" }
});

// Test a webhook
var result = await client.Webhooks.TestAsync("whk_xxx");

// Rotate webhook secret
var rotation = await client.Webhooks.RotateSecretAsync("whk_xxx");

// Delete a webhook
await client.Webhooks.DeleteAsync("whk_xxx");

// List available webhook event types
var eventTypes = await client.Webhooks.ListEventTypesAsync();
foreach (var eventType in eventTypes)
{
    Console.WriteLine($"Event: {eventType}");
}
```

### Receiving events

`Sendly.Webhooks` (the static class, not the `client.Webhooks` resource)
verifies the signature and parses the body. Hand it the raw request body: the
signature covers the exact bytes that were sent, so a re-serialized object no
longer matches.

`WebhookEvent.Data` is a *message* view of `data.object`. That is right for
`message.*` and wrong for everything else: `rcs_brand.*`, `rcs_agent.*`,
`whatsapp_account.*`, `whatsapp_template.*`, `call.*`, `brand.*`, `campaign.*`,
`assignment.*`, `number.*`, `port*`, `conversation.*`, `draft.*`, `contact.*`
and `verification.*` carry a different object entirely, and nothing is raised
when one arrives. `Data` comes back at its defaults, except where a key name
happens to coincide (`id`, `status`), which is worse: it binds another
record's value. Two accessors give you the real payload, and both are
populated for every event type, `message.*` included:

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
// Get account information
var account = await client.Account.GetAsync();
Console.WriteLine(account.Email);

// Check credit balance
var credits = await client.Account.GetCreditsAsync();
Console.WriteLine($"Available: {credits.AvailableBalance} credits");
Console.WriteLine($"Reserved: {credits.ReservedBalance} credits");
Console.WriteLine($"Total: {credits.Balance} credits");

// View credit transaction history
var transactions = await client.Account.ListTransactionsAsync();
foreach (var tx in transactions)
{
    Console.WriteLine($"{tx.Type}: {tx.Amount} credits - {tx.Description}");
}

// List API keys
var keys = await client.Account.ListApiKeysAsync();
foreach (var key in keys)
{
    Console.WriteLine($"{key.Name}: {key.Prefix}*** ({key.Type})");
}

// Get a specific API key
var key = await client.Account.GetApiKeyAsync("key_xxx");

// Get API key usage stats
var usage = await client.Account.GetApiKeyUsageAsync("key_xxx");
Console.WriteLine($"Messages sent: {usage.MessagesSent}");

// Create a new API key
var newKey = await client.Account.CreateApiKeyAsync(new CreateApiKeyOptions
{
    Name = "Production Key"
});
Console.WriteLine($"New key: {newKey.Key}"); // Only shown once!

// Revoke an API key
await client.Account.RevokeApiKeyAsync("key_xxx");

// Rotate a key. Mints a replacement and keeps the old one working for a grace
// period (24-168 hours, default 24) so you can roll the new key out first.
var rotation = await client.Account.RotateApiKeyAsync("key_xxx", gracePeriodHours: 48);
Console.WriteLine(rotation.NewKey.Key);     // full raw sk_ value — shown once!
Console.WriteLine(rotation.NewKey.Warning);
Console.WriteLine(rotation.OldKey.ExpiresAt); // when the old key stops working
Console.WriteLine(rotation.Message);
```

## Error Handling

```csharp
using Sendly.Exceptions;

try
{
    var message = await client.Messages.SendAsync("+15551234567", "Hello!");
}
catch (AuthenticationException e)
{
    // Invalid API key
}
catch (RateLimitException e)
{
    // Rate limit exceeded
    Console.WriteLine($"Retry after: {e.RetryAfter?.TotalSeconds} seconds");
}
catch (InsufficientCreditsException e)
{
    // Add more credits
}
catch (ValidationException e)
{
    // Invalid request
}
catch (NotFoundException e)
{
    // Resource not found
}
catch (NetworkException e)
{
    // Network error
}
catch (SendlyException e)
{
    // Other error
    Console.WriteLine(e.Message);
    Console.WriteLine(e.ErrorCode);
    Console.WriteLine(e.StatusCode);
}
```

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
message.Text         // Message content
message.Status       // queued, sending, sent, delivered, failed
message.CreditsUsed  // Credits consumed
message.CreatedAt    // DateTime
message.UpdatedAt    // DateTime
message.DeliveredAt  // DateTime? (nullable)
message.ErrorCode    // string? (nullable)
message.ErrorMessage // string? (nullable)

// Helper properties
message.IsDelivered  // bool
message.IsFailed     // bool
message.IsPending    // bool
```

## Message Status

| Status | Description |
|--------|-------------|
| `queued` | Message is queued for delivery |
| `sending` | Message is being sent |
| `sent` | Message was sent to carrier |
| `delivered` | Message was delivered |
| `failed` | Message delivery failed |

## Pricing Tiers

| Tier | Countries | Credits per SMS |
|------|-----------|-----------------|
| Domestic | US, CA | 2 |
| Tier 1 | GB, PL, IN, etc. | 8 |
| Tier 2 | FR, JP, AU, etc. | 12 |
| Tier 3 | DE, IT, MX, etc. | 16 |

## Sandbox Testing

Use test API keys (`sk_test_v1_xxx`) with these test numbers:

| Number | Behavior |
|--------|----------|
| +15005550000 | Success (instant) |
| +15005550001 | Fails: invalid_number |
| +15005550002 | Fails: unroutable_destination |
| +15005550003 | Fails: queue_full |
| +15005550004 | Fails: rate_limit_exceeded |
| +15005550006 | Fails: carrier_violation |

## Enterprise

The Enterprise API lets you programmatically manage workspaces, verification, credits, and API keys for multi-tenant platforms. Requires an enterprise master key (`sk_live_v1_master_*`).

### Quick Provision

Create a fully configured workspace in a single call:

```csharp
var client = new SendlyClient("sk_live_v1_master_YOUR_KEY");

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
```

Three provisioning modes:

| Mode | Params | Description |
|------|--------|-------------|
| **Inherit** | `SourceWorkspaceId` | Shares toll-free number from verified workspace |
| **Inherit + New Number** | `SourceWorkspaceId` + `InheritWithNewNumber = true` | Copies business info, purchases new number |
| **Fresh** | `Verification = new VerificationData{...}` | Full business details, new number + carrier approval |

### Workspace Management

```csharp
var ws = await client.Enterprise.Workspaces.CreateAsync("Acme Insurance");
var list = await client.Enterprise.Workspaces.ListAsync();
var detail = await client.Enterprise.Workspaces.GetAsync("ws_xxx");
await client.Enterprise.Workspaces.DeleteAsync("ws_xxx");
```

### Credits & API Keys

```csharp
await client.Enterprise.Workspaces.TransferCreditsAsync("ws_dest", "ws_source", 5000);

var key = await client.Enterprise.Workspaces.CreateKeyAsync("ws_xxx", new CreateWorkspaceKeyOptions
{
    Name = "Production",
    Type = "live"
});
Console.WriteLine(key.Key);

await client.Enterprise.Workspaces.RevokeKeyAsync("ws_xxx", "key_abc");
```

### Webhooks & Analytics

```csharp
await client.Enterprise.Webhooks.SetAsync("https://yourapp.com/webhooks");
var overview = await client.Enterprise.Analytics.OverviewAsync();
var messages = await client.Enterprise.Analytics.MessagesAsync(new EnterpriseAnalyticsOptions { Period = "30d" });
var delivery = await client.Enterprise.Analytics.DeliveryAsync();
```

Full enterprise docs: [sendly.live/docs/enterprise](https://sendly.live/docs/enterprise)

---

## License

MIT
