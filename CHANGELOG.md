# Sendly (.NET)

## 3.29.0

### Minor Changes

- `Contacts.BulkMarkValidAsync(new BulkMarkValidRequest { Ids / ListId })`: clear the invalid flag on many contacts at once (up to 10,000 per call). Escape hatch for when auto-mark misclassifies at scale.
- Four new list-health `Webhook.EventTypes` constants: `ContactAutoFlagged`, `ContactMarkedValid`, `ContactsLookupCompleted`, `ContactsBulkMarkedValid`.
- New `Sendly.Models.ListHealthEventSource` static class with frozen string constants (`SendFailure | CarrierLookup | UserAction | BulkMarkValid`) for the `source` field on auto-flag and mark-valid webhooks.
- `Contact` gains `UserMarkedValidAt` — when a user manually cleared an auto-flag. Carrier re-checks respect this timestamp and leave the contact clean.
- `CheckNumbersResponse` gains `AlreadyRunning` so the client knows when a rapid re-trigger was collapsed against an in-flight lookup.

## 3.28.0

### Minor Changes

- `contacts.MarkValidAsync(id)`: clear the auto-exclusion flag on a contact.
- `contacts.CheckNumbersAsync(new CheckNumbersRequest { ListId, Force })`: trigger a background carrier lookup.
- `Contact` model gains OptedOut, LineType, CarrierName, LineTypeCheckedAt, InvalidReason, InvalidatedAt.

## 3.18.1

### Patch Changes

- fix: webhook signature verification and payload parsing now match server implementation
  - `VerifySignature()` accepts optional `string? timestamp` for HMAC on `timestamp.payload` format
  - `ParseEvent()` handles `data.object` JSON nesting (with flat `data` fallback for backwards compat)
  - `WebhookEvent` adds `bool Livemode`, `JsonElement? Created` properties
  - `WebhookMessageData` renamed `MessageId` to `Id` (with `MessageId` deprecated alias)
  - Added `Direction`, `OrganizationId`, `Text`, `MessageFormat` properties
  - `GenerateSignature()` accepts optional `string? timestamp` parameter
  - 5-minute timestamp tolerance check prevents replay attacks

## 3.18.0

### Minor Changes

- Add MMS support for US/CA domestic messaging

## 3.17.0

### Minor Changes

- Add structured error classification and automatic message retry
- New `ErrorCode` property with 13 structured codes (E001-E013, E099)
- New `RetryCount` property tracks retry attempts
- New `Retrying` status and `message.retrying` webhook event

## 3.16.0

### Minor Changes

- Add `TransferCreditsAsync()` for moving credits between workspaces

## 3.15.2

### Patch Changes

- Add Metadata property to batch message items

## 3.13.0

### Minor Changes

- Campaigns, Contacts & Contact Lists resources with full CRUD
- Template clone method
