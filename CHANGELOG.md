# Sendly (.NET)

## 3.32.0

### Minor Changes

- New `BusinessUpgrade` resource exposes the toll-free entity-upgrade ("fork-with-new-number") flow on the top-level `SendlyClient` as `client.BusinessUpgrade`. Seven methods:
  - `PreflightAsync(PreflightCandidate)` — advisory validation (no writes) of a candidate upgrade payload. Returns `PreflightReport` with `Verdict`, structured `Issues`, and `ProposedFixes`.
  - `BestPrefillAsync()` — "best-of" prefill across the caller's verified workspaces, useful when the current workspace has thin messaging data.
  - `StartAsync(workspaceId, StartUpgradeParams, EinDocumentInput?)` — provisions a new toll-free number + messaging profile under the new entity and submits to the carrier. Multipart upload; EIN doc accepts `Bytes`, `Stream`, or `Path`. Existing number keeps sending during the 1-2 week review window; atomic swap on approval.
  - `StatusAsync(workspaceId)` — reports whether an upgrade is in flight; `Pending` is null when there's none.
  - `CancelAsync(workspaceId)` — idempotent rollback that releases the reserved number, deletes the new messaging profile, and removes the stored EIN document.
  - `ResubmitAsync(workspaceId, StartUpgradeParams, EinDocumentInput?)` — resubmits a rejected upgrade with edits and optionally a new EIN doc.
  - `SetDispositionAsync(workspaceId, DispositionRequest)` — on approval, choose `"moved"` (keep the old number under another workspace via `TargetWorkspaceId`) or `"released"` (return it to the carrier pool).

## 3.31.0

### Patch Changes

- Version bump for unified release. No .NET SDK code changes — this release exists for parity with sibling SDKs that shipped fixes in this cycle (PHP doc/code mismatch, Ruby positional constructor, Rust + Java added `suggest_replies` / `suggestReplies`).

## 3.30.0

### Minor Changes

- `Enterprise.Workspaces.SubmitVerificationAsync(workspaceId, input)`: rewritten to match the actual API shape (camelCase top-level fields, nested `address` / `contact` objects, `EntityType` + `Brn` / `BrnType` / `BrnCountry`). Every property on `VerificationSubmitInput` is now nullable and decorated with `JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)` so unset fields are omitted from the JSON body. The previous shape (non-nullable strings defaulting to `""`) sent empty strings for omitted fields and triggered carrier 400s.
- **Partial-update friendly:** for resubmits on existing workspaces, send only the fields you want to change — everything else is filled from the existing record. Hosted page URLs (`/biz/`, `/opt-in/`, `/legal/`) generated during provision are auto-preserved.
- `Enterprise.Workspaces.ResubmitVerificationAsync(workspaceId, partialUpdates)`: convenience alias for resubmits — same as `SubmitVerificationAsync` but reads more naturally for one-field-change use cases.
- New `Sendly.Models.VerificationSubmitInput` type — type-safe payload shape with all fields documented. The old `SubmitVerificationOptions` name is retained as a back-compat subclass and continues to work.
- `VerificationAddress` gains `Address1` and all properties are now nullable.

### Server-side fixes paired with this release

- `/api/v1/enterprise/workspaces/:id/verification/submit` now returns specific missing-field errors (e.g. `"Missing required fields: website"`) instead of listing every required field whether present or not.
- Endpoint accepts both flat and `{ verification: {...} }` wrapped shapes (matches `/enterprise/provision`).
- `useCase` validation expanded from 23 entries to the full 43-value Telnyx enum.

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
