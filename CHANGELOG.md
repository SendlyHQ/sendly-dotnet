# Sendly (.NET)

## 4.3.0

### Minor Changes

- **Options and fields the API already supported, now in the .NET SDK.**
  - `CreateApiKeyOptions.Type` (`test`, the default, or `live`) and `CreateApiKeyOptions.Scopes`. A live key needs a verified business and a credit balance; the API answers 403 `verification_required` or 402 `credits_required` otherwise. `Account.CreateApiKeyAsync(name)` still creates a test key.
  - `Account.GetAsync()` returns `Organization` (`AccountOrganization`), `Credits` (`AccountCredits`) and `ApiKey` (`AccountApiKey`). `AccountVerification` gains `Status`, `Type`, `Region`, `SubmittedAt` and `UpdatedAt`, and `AccountLimits` gains `MessagesPerMinute`.
  - `Credits` gains `BillingMode` (`prepaid` or `pooled`) and `ReservedBalance`, the same value as `ReservedCredits` under the API's name.
  - `ApiKey` gains `Type`, `Scopes`, `Permissions`, `IsRevoked` and `RevokedAt`. `ApiKeyUsage` gains `KeyId`, `KeyName`, `RecentRequests` (`ApiKeyUsageRequest`, the last 20) and `EndpointBreakdown` (`ApiKeyUsageEndpoint`).
  - `ScheduleMessageRequest` has a parameterless constructor, so `new ScheduleMessageRequest { To = ..., Text = ..., ScheduledAt = ... }` compiles. `ScheduledMessage` gains `Timezone`, `Segments`, `SenderType` and `Metadata` (returned by `ListScheduledAsync`).
  - `Message` gains `Simulated` and `SimulatedReason`, set when a send was simulated rather than delivered to a handset. `Message.SenderTypes` gains `NumberPool`, `Alphanumeric` and `Explicit`, the values the API sends.
  - `Conversation` gains `Channel`, `IsGroup` and `Participants`.
  - `Rule` gains `Enabled`, and `UpdateRuleRequest.Enabled` switches a rule off or on.
  - `Campaign` gains `BatchId`, the message batch that sent it. `CampaignPreview` gains `OptedOutCount`, `InvalidCount`, `CurrentBalance` and `HasEnoughCredits`. `CampaignStatus.Completed` is the status a sent campaign gets.
  - `WebhookTestResult.Message`, and `MessageTemplatePreview` gains `TemplateId`, `CharacterCount` and `SegmentCount`.
  - `CreditTransaction.Types` gains `Transfer`, `AdminGrant` and `AdminSeed`.
  - Enterprise: `Workspaces.InheritVerificationAsync(workspaceId, InheritVerificationOptions)`, where `PurchaseNewNumber = true` orders the workspace its own toll-free number instead of sharing the source's, and `InheritVerificationResponse.NewNumber`. Ordering the number and submitting it for verification are best effort, so check `TollFreeNumber` in the response, which is null when no number could be ordered. `EnterpriseAnalyticsOverview` gains `DeliveryRatePercent` (the rate to two decimal places), `TotalWorkspaces`, `TotalCredits` and `SuspendedWorkspaces`. `EnterpriseCreditsAnalytics` gains `TotalBalance`, `TotalLifetime`, `TotalUsed` and `WorkspaceCount`. `ProvisionWorkspaceResponse` gains `OptInPage` (`ProvisionedOptInPage`) and `LegalPages` (`ProvisionedLegalPages`), each with an `Error` when the page could not be generated.
  - `OwnedNumber.MonthlyCostCentsOrNull` is the monthly cost as the API reported it, or null when the number has no recorded price.
  - `BatchPreviewResponse` gains `Total`, `Sendable`, `Duplicates`, `CreditBalance`, `HasSufficientCredits`, `Pooled`, `KeyType`, `KeyScopes`, `HasWriteScope`, `BlockedMessages` (`BatchPreviewBlockedMessage`), `Compliance` (`BatchPreviewCompliance`) and `Warnings`, the fields the batch preview returns. `ValidationException` has a `(message, statusCode)` constructor.
  - `CallErrorCode` gains `FromNumberNotSupported` (400: calls can only be placed from US and Canadian numbers), `TooManyFailedKeyAttempts` and `TooManyConcurrentVerifications`, and `RcsErrorCode` gains the last two. `RateLimitException` has a constructor that also takes the API's error code.
- **WhatsApp: add numbers by code, profile photos, conversation starters and calling.**
  - `WhatsApp.Signup.CreateAsync(StartWhatsAppSignupRequest)` adds a number to a WhatsApp Business account the workspace already connected, without the Facebook step, when `BusinessAccountId` is set (from `WhatsAppSender.BusinessAccountId`). `VerificationMethod` (`sms`, the default, or `voice`; constants on `WhatsAppVerificationMethod`) says how Meta sends the 6-digit code, and `DisplayName` is optional. The signup comes back `verifying` with an empty `ConnectUrl`. Without `BusinessAccountId` it is the Facebook connection that `CreateAsync(phoneNumber)` starts. The same $19 one-time fee applies, refunded automatically if the connection fails. With `BusinessAccountId` set, a `5xx` (such as `502` `whatsapp_verification_start_failed`, after which the fee is refunded), a `408`, a timeout or a network failure is thrown at once rather than retried, because a retry could start a new session that is charged and, when it fails, refunded; only a `429` the client waits out is retried. Without it, retries are unchanged. A `BusinessAccountId` that is present but empty or whitespace-only throws `ValidationException` before anything is sent, instead of starting a paid Facebook signup.
  - `WhatsApp.Signup.VerifyAsync(id, code)` submits the code and returns the signup, `active` once it is accepted. A `5xx`, a `408`, a timeout or a network failure is thrown at once and never retried automatically, because every submission uses up one of the 5 attempts and a `502` `whatsapp_activation_pending` means WhatsApp already accepted the code; only a `429` the client waits out is retried. `WhatsApp.Signup.ResendAsync(id, verificationMethod)` asks for a new code, at most every 30 seconds (`429` `whatsapp_verification_resend_too_soon` is thrown at once with `RetryAfter`).
  - `WhatsAppSignup` gains `VerificationMethod`, `VerificationAttemptsRemaining` and `VerificationCode`, the texted code once it has arrived on the number. Until a code has been submitted it is the newest code that has arrived since the signup started, so after a resend it is still the earlier code until the new one arrives; once WhatsApp has checked a code, only a code that arrived after the last submission or resend is returned. A submission answered with `502` `whatsapp_verification_unavailable` is not counted, so the same unchecked code can come back, and submitting it again is safe. `WhatsAppSignupSession` gains `PhoneNumber`, `BusinessAccountId`, `FailureReasons`, `VerificationMethod`, `VerificationAttemptsRemaining` and `UpdatedAt`, filled for a number added by code. Signup status can be `verifying`, and the new failure reasons are `verification_start_failed`, `verification_failed` and `verification_expired`.
  - `WhatsApp.Senders.UploadProfilePhotoAsync` (from a file path, or a stream with its file name and content type) and `DeleteProfilePhotoAsync` set and remove a sender's profile photo: a JPEG or PNG of at most 5 MB. A `5xx`, a `408`, a timeout or a network failure from the upload is thrown at once and never retried automatically; only a `429` the client waits out is retried. A stream that can't seek is read into memory first, so an upload retried after a `429` sends the whole photo again.
  - `WhatsApp.Senders.GetConversationalComponentsAsync` and `UpdateConversationalComponentsAsync` read and replace a sender's ice breakers and commands (`WhatsAppConversationalComponents`, `WhatsAppCommand`, `UpdateWhatsAppConversationalComponentsRequest`). Each list you set replaces the stored one, an empty list clears it, and a null list is left out.
  - `WhatsApp.Senders.SetCallingAsync(phoneNumber, enabled)` switches WhatsApp calling on or off and returns `WhatsAppCallingSettings`. Turning it on needs voice on for the number (`409` `voice_not_enabled`). There is no API for placing WhatsApp calls.
  - `WhatsAppSender` gains `BusinessAccountId`, `BusinessName`, `CallingEnabled` and `OutboundCallingAllowed`.
  - `Call` gains `Channel` (`phone`, `whatsapp` or `browser`; constants on `CallChannel`, and any other value comes through unchanged). Call webhooks carry `channel` too.
- `Enterprise.Workspaces.ProvisionBulkAsync` accepts up to 100 workspaces, the API's limit. It refused more than 50 before sending anything.

### Patch Changes

- **Methods that failed on every call now work.** Each was checked against the handler it calls.
  - `Messages.ScheduleAsync` (all three overloads) sent `scheduled_at`, which the API does not read, so every call failed with a 400. It sends `scheduledAt`.
  - `Webhooks.ListAsync` threw `InvalidOperationException` on every call, because the API returns a JSON array.
  - `Conversations.AddLabelsAsync` sent `label_ids`, and `Drafts.CreateAsync` sent `conversation_id` and `media_urls`, so both failed with a 400. They send `labelIds`, `conversationId` and `mediaUrls`. `Drafts.UpdateAsync` sends `mediaUrls` too, which the API used to ignore.
  - `Campaigns.ScheduleAsync` sent `scheduled_at` and failed with a 400. It sends `scheduledAt`.
  - `BusinessUpgrade.SetDispositionAsync` with `moved` sent `target_org_id` and failed with a 400. It sends `targetOrgId`.
  - `Contacts.BulkMarkValidAsync` with a `ListId` sent `list_id` and failed with a 400. It sends `listId`.
  - `Rules.ListAsync` threw `JsonException` whenever a rule's conditions and actions were objects, which is how the API stores rules made in the dashboard or with the other SDKs, or its priority was null. `UpdateAsync` sent a null priority whenever `Priority` was unset, so an update the API accepted saved the rule with no priority and then threw reading it back. A rule made with this SDK was stored as arrays, so it matched every message and applied no label. `Conditions` and `Actions` are now sent as one object each, with the dictionaries in the list merged, and an object is read as a one-element list; a rule stored as arrays still reads as before. A null priority reads as 0.
  - `Enterprise.Analytics.OverviewAsync` threw `JsonException` whenever the delivery rate had decimals, such as 97.53.
  - `Numbers.ListAsync`, `GetAsync`, `UpdateAsync`, `SetDefaultAsync` and `KeepAsync` threw `JsonException` for a number with no recorded monthly cost, such as the toll-free number provisioned with a verification. `MonthlyCostCents` reads 0 for such a number.
  - `Account.GetAsync` returned an empty `Id`, `Email` and `CreatedAt`, because the API nests them under `user` (`CreatedAt` is now read from `user.createdAt`), and a null `Verification` for a workspace without one, so reading `Verification.IsFullyVerified` threw `NullReferenceException`. `Verification` is never null now. `AccountLimits.MessagesPerDay` was always 10000, because it read a snake_case key the API never sends; it now reads the API's limit, which is 100 for a test key and 10,000 for a live key, so code that throttles on it sees 100 with a test key.
  - `Messages.SendGroupAsync` threw `JsonException` after a live group send had gone out and been charged, because a live send lists its recipients as objects (`phoneNumber` and `status`). `To` still holds the phone numbers, and the new `Recipients` (`GroupRecipient`) has each recipient's status. A simulated send lists plain numbers and leaves `Recipients` null.
- **Values that were wrong on every call.**
  - `Message` read snake_case keys the API never sends, so `CreditsUsed`, `IsSandbox`, `SenderType`, `SenderNote`, `CreatedAt`, `DeliveredAt`, `ErrorCode`, `ErrorMessage` and `RetryCount` kept their defaults on `Messages.SendAsync`, `GetAsync`, `ListAsync` and `GetAllAsync`, `Conversations.ReplyAsync` and the messages of `Conversations.GetAsync`. `ErrorMessage` reads the API's `error`.
  - `MessageList.HasMore` was always false, so `Messages.GetAllAsync` stopped after the first page. It reads `pagination.hasMore`, and `GetAllAsync` moves on by the number of messages the API returned, so a `Limit` above the API's maximum of 100 does not skip any.
  - `ScheduledMessage.ScheduledAt`, `CreditsReserved`, `CreatedAt`, `CancelledAt` and `SentAt`, and `CancelScheduledMessageResponse.CreditsRefunded`, were never filled.
  - `Credits.AvailableBalance` and `ReservedCredits` were always 0, so `HasCredits` was always false.
  - `ApiKey.IsActive` was true for revoked keys, and `CreatedAt`, `LastUsedAt` and `ExpiresAt` were empty. `ApiKeyUsage.TotalRequests`, `CreditsUsed` and `LastRequestAt` were 0 or null. `CreateApiKeyResponse.ApiKey` was always empty.
  - `Conversation.PhoneNumber`, `UnreadCount`, `MessageCount`, `LastMessageText`, `LastMessageAt`, `LastMessageDirection`, `ContactId`, `CreatedAt` and `UpdatedAt`, `ConversationContextResponse.TokenEstimate` and the counts of its conversation, `PaginationInfo.HasMore` on conversation lists and message pages, and `Label.CreatedAt` read defaults. So did `Draft.ConversationId`, `MediaUrls`, `CreatedBy`, `ReviewedBy`, `ReviewedAt`, `RejectionReason`, `MessageId`, `CreatedAt` and `UpdatedAt`.
  - `Campaign.RecipientCount`, `SentCount`, `DeliveredCount`, `FailedCount`, `EstimatedCredits`, `CreditsUsed`, `ScheduledAt`, `StartedAt` and `CompletedAt` were 0 or null on every campaign method. Every method but `SendAsync` now reads them from the campaign the API returns. `CampaignPreview.RecipientCount` and `EstimatedCredits` were 0, and `BlockedCount` and `SendableCount` null.
  - `Campaigns.SendAsync` also returned an empty `Id`: the API answers with the message batch the send created, not the campaign. The campaign it returns now has the `Id` you passed and, from the batch, `BatchId`, `RecipientCount`, `SentCount`, `FailedCount` and `CreditsUsed`; `Status` is still the batch's status. Call `GetAsync` for the campaign's name, text and dates.
  - `Contacts.ImportAsync` sent `list_id` and `opted_in_at`, which the API ignores, so contacts were never added to the list and got no batch opt-in date, and `SkippedDuplicates` and `TotalErrors` were 0. `Contacts.CheckNumbersAsync` checked every contact instead of the list you named, and `AlreadyRunning` was always false.
  - `MessageTemplates.PreviewAsync` left `PreviewText` empty: the API returns `rendered_text`.
  - `Webhooks.TestAsync` left `StatusCode` and `ResponseTimeMs` at 0: the API reports them on the test delivery.
- **Request bodies leave out properties you did not set.** Every property you left null used to be sent as JSON null, so an update cleared the fields you left out: `Contacts.UpdateAsync` with only `Name` erased the email and metadata, `Contacts.Lists.UpdateAsync` with only `Name` erased the description, `Drafts.UpdateAsync` with only `Text` erased the draft's metadata, and `Enterprise.Workspaces.UpdateOptInPageAsync` erased the logo, colours, headline and benefits you did not pass. `Conversations.UpdateAsync` with a single field failed, and so did `Drafts.UpdateAsync` without `Text`, and `Rules.UpdateAsync` unless it set `Name`, `Conditions` and `Actions`. The API changes only the fields a request carries. Setting `UpdateContactRequest.Name`, `Email` or `Metadata`, `UpdateContactListRequest.Description`, `UpdateDraftRequest.Metadata` or any `UpdateOptInPageOptions` property to null still sends null and clears the field, as before, and setting `UpdateDraftRequest.MediaUrls` to null now removes the draft's media. `UpdateQuotaOptions.MonthlyMessageQuota` is sent even when you leave it unset, because null removes the quota.
- **`Messages.PreviewBatchAsync` reads the preview the API returns.** It read names the API never sends, so `TotalMessages`, `WillSend`, `CurrentBalance`, `HasEnoughCredits` and `CanSend` were 0 or false on every call; only `Blocked` and `CreditsNeeded` were filled. The response now reads these fields the preview returns (see Minor Changes), and fills the old properties from them. `CanSend` is true when at least one message passes, the batch has at most 10,000 messages, every blocked message is an opt-out (a live send skips those but rejects the whole batch for any other block), the key has `sms:send`, and the balance covers it or the key is a test key. The preview does not check the monthly quota or a suspended workspace, and a test send skips the destination checks the preview applies. `BlockReasons` counts the blocked messages by reason.
- **An id that is empty, `.` or `..` is refused before any request is sent.** Every method that puts an id in the request path throws a `ValidationException` for one. Percent-encoding cannot protect such an id, because the URL is resolved with it read as a dot-segment: `Enterprise.Workspaces.RevokeKeyAsync("ws_1", "..")`, `DeleteOptInPageAsync` and `CancelInvitationAsync` sent `DELETE /enterprise/workspaces/ws_1/`, the request that deletes the workspace, and `Contacts.Lists.RemoveContactAsync("list_1", "..")` sent the request that deletes the list. An id with dots inside it, such as `key.v1`, is sent as before.
- **A `ValidationException` from a 422 reports status 422.** It reported 400 for every validation error. `ValidationException` has a constructor that takes the status.
- **A retried 5xx keeps its idempotency key.** After a 5xx the client sent the retry with a new auto-generated key, a leftover from when the API recorded server errors under the key. The API has not recorded a 5xx since August, so the retry runs again under the same key either way. A new key only lost protection in one case: when the API had finished the request and recorded its answer but a gateway returned the 5xx, a retry with a new key sent the message again. The retry now carries the same key, so that case returns the recorded answer instead. This applies to uploads too.
- **A `too_many_failed_key_attempts` 429 is thrown at once.** It means too many requests from this address used a wrong API key for the account, so its keys, even a correct one, are refused from this address until `Retry-After` has passed, which can be up to 300 seconds. The client used to wait that out and retry: a client using the wrong key then got a 401, and a client with the right key, locked out by a wrong key from the same address, got through after the wait. It now gets the exception at once. It is now thrown on the first attempt as a `RateLimitException` with `ApiErrorCode` `too_many_failed_key_attempts`, the API's message and `RetryAfter`. The next entry says which 429s are still retried.
- **A 429 is waited out only when waiting can help, and never for more than a minute.** The client waited out and retried every 429 other than the key lockout. It now retries only an ordinary `rate_limit_exceeded`, the per-minute `provision_rate_limit` from enterprise workspace provisioning (120 a minute), a 429 with no code, or `too_many_concurrent_verifications` (too many first-time API key checks at once), and only when the wait is 60 seconds or less. It then sends again after exactly that wait, with no backoff added. `RateLimitException.RetryAfter` falls back to the body's `retryAfter` when there is no `Retry-After` header. It was null for those, including `Verify.SendAsync` and `ResendAsync` against the per-phone limit (5 codes per 10 minutes) or the daily limit (20 per day), which were sent three more times and are now thrown at once with the wait set while more than a minute of the limit remains. After the last attempt the client throws without waiting. Other final 429s, such as `max_attempts_exceeded` from `Verify.CheckAsync`, `daily_call_limit`, `quota_exceeded` and the hourly `provision_rate_limit`, are thrown on the first attempt; they were retried for about 7 seconds.
- **A 4xx answer other than 408 or 429 is thrown on the first attempt.** 400, 401, 402, 404 and 422 already were. Any other, such as a 409 conflict or the 403 `verification_required` that `CreateApiKeyAsync` gets for a live key before the business is verified, used to be sent three more times over about 7 seconds, and the API gave the same answer each time: it treats a 4xx as final, and replays it for a request that carries the same idempotency key. Network failures, timeouts, 408s and 5xx responses are retried as before; the 429 entries above say which 429s are.
- **Docs that described behaviour the API does not have.** `Webhooks.BackfillAsync` no longer says synthesized events have fresh ids or to dedupe on `data.object.id`: they carry the event id the original dispatch used, so dedupe on `event.id`. `Webhooks.TestAsync` says a failed test throws a `ValidationException` with the API's message. `ListMessagesOptions.Limit` defaults to 50, not 20. `ScheduleMessageRequest.ScheduledAt` and `Messages.ScheduleAsync` say a message can be scheduled from 5 minutes to 5 days ahead, not from 1 minute; the API refuses any other time with a 400 `invalid_scheduled_time`. `CampaignStatus.Sent` and `Paused` and `CreditTransaction.Types.Adjustment` say they are never returned. Members the API never sends say so: `Credits.PendingCredits`, `Account.Name` and `CompanyName`, `AccountVerification.EmailVerified`, `PhoneVerified` and `IdentityVerified`, `AccountLimits.MessagesPerSecond` and `MaxBatchSize`, `ApiKeyUsage.SuccessfulRequests` and `FailedRequests`, `Message.UpdatedAt`, `Campaign.TemplateId`, `CampaignPreview.EstimatedCost`, `MessageTemplatePreview.Id`, `Name` and `Variables`, `CancelScheduledMessageResponse.CancelledAt` and `EnterpriseCreditsAnalytics.Data`.
- **WhatsApp docs match the API.** The XML docs now say that the API never sends the `expired` signup status, that a closed window returns its past `ExpiresAt` rather than null, that a media send returns its caption as `Text`, how in-window replies are priced (1 credit for the first 1,000 per sending number each month, then the destination's utility price), which roles and scopes connecting and editing need, the `waba_mismatch` and `registration_timeout` failure reasons, `template_header_variable_unsupported`, `whatsapp_unavailable` (503), `whatsapp_signup_limit_reached` (429), and `whatsapp_send_failed` as a final 422 or a retried 502. Nothing changes at runtime.
- **WhatsApp send failures, documented.** A `502` `whatsapp_send_failed` means the message provably never reached the carrier, so it was not sent and is safe to send again. A `409` `whatsapp_send_unconfirmed` means the outcome is unknown: the message was marked failed and refunded but may still be delivered, so check before sending it again (it could arrive twice). It is not retried automatically. `WhatsAppSenderProfile.ProfilePhotoUrl` no longer says the photo can't be changed through the API, and `CallErrorCode.VoiceNotEnabled` notes its `409` from `SetCallingAsync`.

### Worth knowing before you upgrade

Nothing was removed or renamed, and no member changed type. A few things can still catch you out:

- `Message.SenderTypes.User`, `Api`, `System` and `Campaign` are `[Obsolete]`: the API never sends those values, so a comparison against them never matched. A build that treats warnings as errors (`CS0618`) needs to move to `NumberPool`, `Alphanumeric` or `Explicit`.
- An update no longer clears the fields you leave unset: `new UpdateContactRequest { Name = "Ann" }` used to erase the contact's email and metadata too, and now changes only the name. To clear a field, set it to null; the properties that can be cleared this way are listed under Patch Changes. Any other request property set to null is left out, like one you never set.
- `Messages.GetAllAsync` now pages, so a loop over it sees every message rather than only the first page.
- `AccountVerification.IsFullyVerified` is true when the business verification's `Status` is `verified`, or `approved`, which is what a business verified for international sending reports; it used to be false or throw.
- `EnterpriseAnalyticsOverview.DeliveryRate` stays an `int`, rounded; read `DeliveryRatePercent` for the two decimal places. `OwnedNumber.MonthlyCostCents` stays an `int` and reads 0 when no price is recorded; `MonthlyCostCentsOrNull` tells that apart from a free number.
- `Rule.Conditions` and `Rule.Actions` hold one dictionary for a rule the API stores as an object. When you send several dictionaries their keys are merged into one object, so a key in a later dictionary replaces the same key in an earlier one.
- `BatchPreviewResponse.CanSend` was always false and is now derived from the preview, so code that refused to send on it will now send.
- A `ValidationException` from a 422 reports `StatusCode` 422, not 400.
- 429s other than an ordinary `rate_limit_exceeded`, the per-minute `provision_rate_limit` or `too_many_concurrent_verifications` of a minute or less, such as the hourly `provision_rate_limit`, `max_attempts_exceeded` or the per-phone OTP limit, are thrown at once, and `RetryAfter` can be up to 600 or 86,400 seconds. Code that relied on the client to wait them out needs to handle the exception.
- The new `Workspaces.InheritVerificationAsync(string, InheritVerificationOptions)` overload makes a call that passes a literal `null` as the source workspace ambiguous; such a call always threw a `ValidationException`.

## 4.2.0

### Minor Changes

- **Configure voice from code on `client.Voice`.** Everything a call depends on used to be dashboard-only; it is now on three sub-resources:
  - `Voice.Numbers.ListAsync()`, `GetAsync(number)`, `UpdateAsync(number, UpdateVoiceNumberRequest, IdempotentRequestOptions?)` and `RegisterEmergencyAddressAsync(number, EmergencyAddress, IdempotentRequestOptions?)`. `number` is the number's id or its E.164 phone number, percent-encoded in the path (`+15555550188` is sent as `%2B15555550188`). `UpdateAsync` switches voice on or off and chooses how the number answers (`VoiceMode.None`, `VoiceMode.RingDashboard` or `VoiceMode.Agent` with `AgentId`); this changes how real phone calls to the number are answered. An emergency address is required before a US or Canadian number can place calls, and the first registration adds $1.50 a month to the number.
  - `Voice.Agents.ListAsync()`, `CreateAsync(CreateVoiceAgentRequest, IdempotentRequestOptions?)`, `GetAsync(id)`, `UpdateAsync(id, UpdateVoiceAgentRequest, IdempotentRequestOptions?)` and `DeleteAsync(id, IdempotentRequestOptions?)`. An agent answers real callers on any number pointed at it. Each agent holds its own scoped sending key (`VoiceAgent.CanSendSms`), a workspace can have 20 agents (409 `agent_limit`), and an agent that still answers a number can't be deleted (409 `agent_in_use`).
  - `Voice.Voices.ListAsync()` lists the voices an agent can speak with.

  Reads need the `calls:read` scope and writes `calls:write` with a live key. POSTs get an automatic `Idempotency-Key`; `UpdateAsync` and `DeleteAsync` send one only when you pass `IdempotentRequestOptions`. In a team workspace, number and emergency-address writes also need a role that can change settings and agent writes a role that can manage API keys (403 `forbidden`). Every operation answers 404 (`voice_not_enabled`, a `NotFoundException`) until voice is enabled for your workspace.

  New types in `Sendly.Resources`: `VoiceResource`, `VoiceNumbersResource`, `VoiceAgentsResource`, `VoiceVoicesResource`, `VoiceNumber`, `VoiceNumberListResponse`, `VoiceNumberEmergencyAddress`, `EmergencyAddress`, `VoiceNumberRates`, `UpdateVoiceNumberRequest`, `VoiceAgent`, `VoiceAgentListResponse`, `VoiceAgentTools`, `VoiceAgentToolsInput`, `CreateVoiceAgentRequest`, `UpdateVoiceAgentRequest`, `DeletedVoiceAgent`, `Voice`, `VoiceListResponse`, and the string-constant class `VoiceMode`. `CallErrorCode` gains `AgentInUse`, `AgentLimit`, `InvalidVoiceMode`, `InvalidAddress`, `E911NotApplicable`, `VoiceAttachFailed`, `CarrierRefused` and `VoiceUnavailable`. The README's **Voice Calls** section has a new **Configure voice** subsection.

- **`SendlyException.ResponseBody`** holds the JSON object an error response carried, so fields beyond `error` and `message` are reachable: the `numbers` a 409 `agent_in_use` lists, and the `suggested` address (or null) on a 422 `invalid_address`. It is null when the error did not come from a JSON object response.

### Patch Changes

- **Recording channels were documented the wrong way round.** `Calls.RecordingAsync` and `CallRecording.ContentType` said an agent call's recording had the caller on the left channel and the agent on the right. The agent is on the left channel and the other party on the right.

### Not changed in this release

- No public members were deprecated, renamed or removed.

## 4.1.0

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
