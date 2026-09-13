# Sendly (.NET)

## Unreleased

### Minor Changes

- **Voice calls on `client.Calls`.** Place a phone call handled by one of your AI agents, list and inspect calls, end a call, and download its recording. Five new operations: `Calls.CreateAsync(CreateCallRequest, IdempotentRequestOptions?)`, `Calls.ListAsync(ListCallsOptions?)`, `Calls.GetAsync(id)`, `Calls.HangupAsync(id, IdempotentRequestOptions?)` and `Calls.RecordingAsync(id)`. Both writes take an optional `IdempotentRequestOptions` and get an automatic `Idempotency-Key`, as elsewhere; ids are percent-encoded in the path. Reads need an API key with the `calls:read` scope, writes `calls:write` and a live key (`403 live_key_required` on a test key). Every operation answers 404 (`voice_not_enabled`, a `NotFoundException`) until voice is enabled for your workspace.

  `GetAsync` returns the transcript for agent-handled calls (`Call.Transcript`, a list of `CallTranscriptLine`; null for other calls). `RecordingAsync` returns a `CallRecording` whose `Url` and `ExpiresAt` are set only while `Status` is `ready`; the link is signed and valid for five minutes.

  New types in `Sendly.Resources`: `Call`, `CallTranscriptLine`, `CreateCallRequest`, `ListCallsOptions`, `CallListResponse` (with `CallListPagination`), `CallRecording`, and the string-constant classes `CallStatus`, `CallDirection`, `CallKind`, `CallHandledBy`, `CallBilling`, `CallRecordingStatus`, `CallHangupClass` and `CallErrorCode`. The README's new **Voice Calls** section covers pricing, the dashboard prerequisites and each refusal code.

- **`OwnedNumber.VoiceEnabled` and `OwnedNumber.VoiceMode`** on `Numbers.ListAsync()` items, so you can find a number to call from: `VoiceMode` is `none`, `ring_dashboard` or `agent`. Both are null where the API omits them.

### Not changed in this release

- No public members were deprecated, renamed or removed.

## 4.0.0

### Major Changes

- Every Sendly SDK, the CLI and the MCP server now share one version. No public API was removed or changed in this package; the major aligns the fleet and carries the behaviour change below.

### Security

- **Path parameters are percent-encoded.** Every id you pass is now encoded (`Uri.EscapeDataString`) before it goes into the request path. An id containing `/`, `?` or `#` used to change which endpoint the request reached: an id of `../../account/keys` left its collection and hit another endpoint carrying your API key. Ordinary ids are sent byte-for-byte as before.

## 3.40.0

### Minor Changes

- **Lifecycle webhook payloads are now reachable.** `Webhooks.ParseEvent` built a `WebhookMessageData` out of every `data.object`, which is right for `message.*` and wrong for every other event. `rcs_brand.*`, `rcs_agent.*`, `whatsapp_account.*`, `whatsapp_template.*`, `call.*`, `brand.*`, `campaign.*`, `assignment.*`, `number.*`, `port*`, `conversation.*`, `draft.*`, `contact.*` and `verification.*` carry a different object entirely, and because the message view is filled property by property from keys those payloads do not have, `WebhookEvent.Data` came back at its defaults — `Id` empty, `Segments` 1, `Direction` `"outbound"` — and nothing was thrown. Where a key name happened to coincide, such as `id` or `status`, it was worse than empty: it bound another record's value. An integration looked healthy while silently dropping `agent_id` and `stage`. `WebhookEvent` gains two accessors, both populated for every event type, `message.*` included:
  - `RawObject` — `data.object` as a `JsonElement`, exactly as it arrived.
  - `ObjectAs<T>(JsonSerializerOptions? options = null)` — deserializes `data.object` into a type you declare.

  `Data` is unchanged and still holds the message for `message.*` events, so existing message handlers keep working. `ParseEvent` always sets `RawObject`, so `ObjectAs<T>()` throws `InvalidOperationException` only on a `WebhookEvent` you constructed yourself. The README's new "Receiving events" section under **Webhooks** works an `rcs_agent.live` handler end to end.

- **`Webhook.EventTypes` now names every event the API emits.** Twenty-nine constants were missing, so there was no typed way to subscribe to RCS, WhatsApp, voice, verification, conversation or draft events: `message.read`, `message.opt_in`, `message.opt_out`, the six `verification.*`, `conversation.created` / `conversation.updated`, the three `draft.*`, `rcs_brand.verified` / `rcs_brand.failed`, the four `rcs_agent.*`, `whatsapp_account.connected` / `whatsapp_account.failed`, the three `whatsapp_template.*`, and `call.started` / `call.completed` / `call.recording.ready`. `scripts/check-webhook-event-parity.mjs` now diffs the list against the server's in CI, so it cannot drift again. Nothing was renamed or removed; the constants you already use are untouched.

### Patch Changes

- **A response that is not JSON now throws a `SendlyException` instead of a bare `JsonException`.** On the success path the body went straight into `JsonDocument.Parse`, so a `BaseUrl` pointing at the origin rather than `https://sendly.live/api/v1`, or a proxy or security product answering in the API's place, surfaced as an unexplained JSON parse error. The exception now names both likely causes, carries the HTTP status, and quotes the first 200 characters of the body, which makes an intercepted request readable from the message alone. The error path already fell back to the raw body and is unchanged.
- **The `User-Agent` reported the wrong version.** The client sent `sendly-dotnet/3.37.1` from 3.37.1 through 3.39.0, because the release script updated the csproj but not the `SendlyClient.Version` constant it is built from. Both move together now and 3.40.0 reports `sendly-dotnet/3.40.0`. Nothing in the API keys off this header, so the effect was confined to your own request logs.

### Worth knowing before you upgrade

Nothing was removed, renamed or deprecated, and no existing call changes behaviour. Three things can still catch you out:

- `ObjectAs<T>()` uses the `System.Text.Json` defaults, which apply no naming policy, so a property named `AgentId` does not bind to the wire's `agent_id`. Mark each field with `[JsonPropertyName]`, or pass your own `JsonSerializerOptions`. It also deserializes rather than validates: a field the payload never carried comes back at its default, which is the same silence this release set out to remove, so check the fields you depend on. `WebhookVerificationData` ships with the SDK and already carries the right attributes for `verification.*` events.
- `WebhookEvent.Data` is still populated for lifecycle events, at defaults, because emptying it would be a breaking change. On `contact.auto_flagged` that is actively misleading: `data.object` is a contact, so `Data.Id` holds the **contact** id and a handler keyed on it acts on the wrong record (the contact's own id, not the `message_id` the payload also carries). Read lifecycle payloads through `RawObject` or `ObjectAs<T>()`.
- `message.queued` and `message.undelivered` have never been emitted by the API and are rejected with a 400 on subscribe. `Webhook.EventTypes` has never listed them, so there is nothing to migrate off here, but drop them from any `Events` list you build out of raw strings.

## 3.39.0

### Minor Changes

- **Self-serve RCS registration on `client.Rcs`.** Draft a brand and an agent, invite test devices, submit for review by Sendly, and request launch, all from the API. Sendly reviews the registration and passes it to the carrier network; the API mirrors what the dashboard can do (approval and launch remain with Sendly). Ten new operations, nested the way `Agents` already is: `Rcs.Registration.GetAsync()`, `Rcs.Dossier.GetAsync()`, `Rcs.Brands.CreateAsync(...)` / `UpdateAsync(...)`, and `Rcs.Agents.CreateAsync(...)` / `GetAsync(...)` / `UpdateAsync(...)` / `SetTestDevicesAsync(...)` / `SubmitAsync(...)` / `RequestLaunchAsync(...)`. Every write takes an optional `IdempotentRequestOptions`; POSTs also get an automatic key, as elsewhere. Registration calls need an API key with the `rcs:read` / `rcs:write` scopes and, like the rest of RCS, answer 404 (`rcs_not_enabled`, a `NotFoundException`) until the `rcs_channel` flag is on for your account.

  Assets can't be uploaded over the API: `LogoUrl`, `HeroUrl` and `CallToActionMediaUrl` must already be public `https://` URLs (422 `rcs_invalid_content` otherwise). Upload files from the dashboard.

  New types in `Sendly.Resources`: `RcsBrandInput` (with `RcsBrandAddressInput`, `RcsBrandContactInput`), `RcsBrand` (with `RcsBrandAddress`, `RcsBrandContact`), `RcsBrandResponse`, `CreateRcsAgentRequest`, `UpdateRcsAgentRequest` (with `ClearCampaign` / `ClearTesting` to remove a section), `RcsAgentBasicsInput` / `RcsAgentBasics` (with `RcsAgentPhoneContact`, `RcsAgentWebsiteContact`, `RcsAgentEmailContact`), `RcsCampaign`, `RcsInteraction`, `RcsConsentSettings`, `RcsOptInMethod`, `RcsTesting`, `RcsAgentDetail`, `RcsAgentResponse`, `RcsAgentDetailResponse`, `RcsAgentReviewResponse`, `RcsTestDevice`, `RcsTestDeviceInput`, `RcsTestDeviceListResponse`, `RcsRequestLaunchRequest`, `RcsRegistration`, `RcsDossier`, and the string-constant classes `RcsCustomerStage`, `RcsReviewStatus`, `RcsErrorCode`, `RcsLegalEntityType`, `RcsOrganizationType`, `RcsAgentUseCase`, `RcsInteractionType`, `RcsOptInMethodType`, `RcsDossierSource`.

- **`RcsAgent.Stage`** on `Rcs.Agents.ListAsync()` items: where each agent sits in the registration journey (`RcsCustomerStage`).

- **`SendlyException.ApiErrorCode` and `FieldErrors`.** Every mapped exception now carries the response body's `error` string (e.g. `rcs_field_locked` vs `rcs_launch_not_ready`, both 409s) and its `errors` array as `SendlyFieldError` (`Path` + `Message`). `ErrorCode` is unchanged and still returns the per-class constant. Populated for every resource, not just RCS.

- `PATCH` and `PUT` requests can now carry a caller-supplied `Idempotency-Key` (used by the RCS registration writes). No key is generated automatically for those verbs; the existing methods behave exactly as before.

### Not changed in this release

- No public members were deprecated, renamed or removed. `Rcs.Agents.ListAsync()` and `Rcs.CapabilityAsync(...)` are untouched.

## 3.38.0

### Minor Changes

- **Automatic idempotency keys on every POST.** The client now generates an `Idempotency-Key` (`sendly-dotnet-retry-<guid>`) once per logical request and reuses it across its own timeout and network-error retries, so on endpoints that support idempotency the server recognises a retry of a request that already reached it and returns the original response instead of executing it again. This narrows the duplicate-send and double-charge window that timeout retries used to open, it does not close it: the server records a key only once the original request has finished, so a retry that fires while the first request is still running is not recognised as a repeat. Keys are rotated after a real 5xx (the outcome is known, so the retry should re-execute) and preserved across timeouts and connection failures (the outcome is unknown, so the server can dedupe). Multipart uploads carry a key on the same terms: `Media.UploadAsync`, the EIN document on `BusinessUpgrade.StartAsync` / `ResubmitAsync`, and enterprise verification-document uploads. GET, PUT, PATCH and DELETE are unaffected.
- **`Messages.SendBatchAsync` deliberately sends no automatic key.** The batch endpoint already dedupes header-less retries server-side by hashing the request contents, and an automatic per-attempt key would bypass that protection. You can still pass your own key.
- **Caller-supplied idempotency keys.** New overloads take an `IdempotentRequestOptions` after the request: `Messages.SendAsync` (SMS, WhatsApp and RCS), `Messages.SendGroupAsync`, `Messages.SendBatchAsync` and `Messages.ScheduleAsync`. Your key is sent verbatim and never rotated, and repeating a request with the same key within 24 hours returns the original response instead of executing again. Keys are validated before any network call (1 to 255 printable ASCII characters, otherwise `ValidationException`); empty and whitespace-only values are treated as absent, so the automatic key still applies. One caveat worth knowing up front: reusing a key with a different request body is rejected by the server with a 422, and this SDK still maps 400 and 422 onto the same `ValidationException`, so for now you have to match on the message to tell a key conflict from a validation failure.
- **Batch responses now decode.** The batch endpoints send camelCase field names and the model was reading snake_case, so `BatchId` came back as an empty string, `CreditsUsed` as 0, and `CreatedAt` / `CompletedAt` as null on every batch call. The names now match the wire. `GetBatchAsync` and `ListBatchesAsync` return the batch id as `id` rather than `batchId`, which is read as a fallback, so `BatchId` is populated there too.
- `BatchMessageResponse` gains the fields the send endpoint actually reports: `Sent` (messages handed off for delivery), `OptedOutSkipped`, `InvalidSkipped` and `CreditsRefunded`. Use `Sent`, not the queued count, to see how much of a batch went out.
- `BatchMessageResponse.Queued` is replaced by `QueuedCount`, which is `int?`. Only the batch read and list endpoints report a queued count; a send response does not include one, and it is now null there instead of a misleading 0.
- **The `Template` model matches what the API returns**, which it did not before. `Body` reads the `text` field (it was previously always empty), and the variables list is a list of objects with a key, type and fallback rather than a list of strings, so any template that declared a variable threw a JSON exception on the way in rather than returning a value. `Template` gains `Status`, `Version`, `PresetSlug` and `PublishedAt`, and `IsPreset` is now a real field read from the response instead of being derived from a `Type` string the API never sent. `CreateTemplateRequest` and `UpdateTemplateRequest` send `text` and an optional list of variable definitions, and omit anything you leave unset.
- **Members removed in the model rewrites are restored and marked `[Obsolete]`.** Code written against the previous release still compiles; you will now see `CS0618` deprecation warnings pointing at the replacement. None of them are serialised, so they cannot leak into a request body.
  - `BatchMessageResponse.Queued` (use `QueuedCount`). It returns 0 when the response reports no queued count, so it cannot tell "zero queued" apart from "not reported".
  - `Template.Variables` as `List<string>` (use `VariableDefinitions`, which also carries each variable's type and fallback). Reading it returns a fresh copy of the keys, so an in-place change such as `template.Variables.Add(...)` is discarded. Assign a whole list, or edit `VariableDefinitions` directly.
  - `Template.Type` (use `IsPreset`, or `PresetSlug` for which preset a template came from).
  - `Template.IsPublished` (use `Status`, which is `"draft"` or `"published"`, or `PublishedAt`).
  - `Template.IsDefault` is **permanently empty**: templates have no default flag, so it is never populated from a response and never sent. It holds only what you assign. Use `IsPreset` to tell built-in templates from your own.
  - `CreateTemplateRequest.Locale` and `UpdateTemplateRequest.Locale` are **never sent**. Templates are not locale-scoped. Create one template per locale.
  - `CreateTemplateRequest.IsPublished` is **never sent**: templates are always created as drafts. Call `Templates.PublishAsync` afterwards.
  - `UpdateTemplateRequest.IsPublished` is **never sent**: publishing is a separate call. Use `Templates.PublishAsync`.
  - Not deprecated but worth the same warning: `Template.Locale` is never populated from a response either, for the same reason.

### Patch Changes

- **A default-constructed client could not reach the API at all, and now can.** Request paths are relative and the base address did not end in a slash, so reference resolution dropped the version segment: with the default base URL `https://sendly.live/api/v1`, a call to `messages` resolved to `https://sendly.live/api/messages` and came back 404. That applied to every request, not one endpoint. Base URLs are now normalised, so a custom `BaseUrl` of `https://your-host/api/v1` works whether or not you wrote the trailing slash, and an existing trailing slash is not doubled. If you already worked around this by adding the slash yourself, nothing changes for you.
- **`client.Templates` was pointed at a prefix the API does not serve, so none of it could ever have worked.** `ListAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` and `PublishAsync` all requested `/verify/templates...`, which has no route at any version and returned 404 every time. They now use `/templates...` and really execute, including the writes. If you built around these methods failing, that code is now live.
- `client.Templates` and `client.MessageTemplates` are two views of the same `/templates` resource, a slimmer one and a fuller one, not two separate template systems. The docs previously described `client.Templates` as managing Verify OTP templates under `/verify/templates`, which was wrong on both counts.
- **`Account.ListTransactionsAsync()` requested `/account/transactions`**, which does not exist, so it 404'd. It now reads `/credits/transactions` and returns your credit ledger.
- **`Account.RevokeApiKeyAsync(id)` sent `DELETE /account/keys/{id}`**, a path registered for GET only, so the call 404'd and the key was never revoked. Revocation is now `PATCH /account/keys/{id}/revoke`, the verb the server accepts. If you called this and assumed a key was dead, check it. The key you are currently authenticating with still cannot be revoked.
- **`Account.ListApiKeysAsync()` always returned an empty list.** It looked for an `api_keys` array and the endpoint returns `keys`. Both envelopes are accepted now, so the call returns your keys.
- **Still broken, so you do not go hunting.** `Templates.UnpublishAsync` still calls `/verify/templates/{id}/unpublish`; there is no unpublish route anywhere on the API to repoint it at, so it continues to 404. `Templates.CloneAsync` and `MessageTemplates.CloneAsync` call `/templates/{id}/clone`, which exists only as an unversioned, session-authenticated route, so both still 404 for an API key. `ListTemplatesOptions.Limit`, `Type` and `Locale` are sent as query parameters that the list endpoint ignores; it returns every template regardless.

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
- `useCase` validation expanded from 23 entries to the full 43-value carrier use-case enum.

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
