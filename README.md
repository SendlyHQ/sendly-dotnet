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
<PackageReference Include="Sendly" Version="1.0.5" />
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
    BaseUrl = "https://api.sendly.live/v1",
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
Console.WriteLine($"Queued: {batch.Queued}");
Console.WriteLine($"Failed: {batch.Failed}");
Console.WriteLine($"Credits used: {batch.CreditsUsed}");

// Get batch status
var status = await client.Messages.GetBatchAsync("batch_xxx");

// List all batches
var batches = await client.Messages.ListBatchesAsync();
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

## Webhooks

```csharp
// Create a webhook endpoint
var webhook = await client.Webhooks.CreateAsync(new CreateWebhookRequest
{
    Url = "https://example.com/webhooks/sendly",
    Events = new[] { "message.delivered", "message.failed" }
});

Console.WriteLine(webhook.Id);
Console.WriteLine(webhook.Secret); // Store securely!

// List all webhooks
var webhooks = await client.Webhooks.ListAsync();

// Get a specific webhook
var wh = await client.Webhooks.GetAsync("whk_xxx");

// Update a webhook
await client.Webhooks.UpdateAsync("whk_xxx", new UpdateWebhookRequest
{
    Url = "https://new-endpoint.example.com/webhook",
    Events = new[] { "message.delivered", "message.failed", "message.sent" }
});

// Test a webhook
var result = await client.Webhooks.TestAsync("whk_xxx");

// Rotate webhook secret
var rotation = await client.Webhooks.RotateSecretAsync("whk_xxx");

// Delete a webhook
await client.Webhooks.DeleteAsync("whk_xxx");
```

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
var transactions = await client.Account.GetCreditTransactionsAsync();
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
| Domestic | US, CA | 1 |
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

## License

MIT
