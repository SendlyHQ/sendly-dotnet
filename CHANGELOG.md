# Sendly (.NET)

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
