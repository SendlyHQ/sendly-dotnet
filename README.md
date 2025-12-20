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
// Simple
var message = await client.Messages.SendAsync("+15551234567", "Hello!");

// With request object
var message = await client.Messages.SendAsync(new SendMessageRequest(
    "+15551234567",
    "Hello from Sendly!"
));

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
| +15550001234 | Success |
| +15550001001 | Invalid number |
| +15550001002 | Carrier rejected |
| +15550001003 | No credits |
| +15550001004 | Rate limited |

## License

MIT
