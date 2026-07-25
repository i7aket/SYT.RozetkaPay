# API Compatibility Matrix

## Scope

- SDK: `SYT.RozetkaPay` (`0.1.0-alpha.1`)
- API path family: `v1` (`/api/*/v1/*`)
- OpenAPI schema version: `3.0.3`

## Source of Truth

- Official public docs: `https://cdn.rozetkapay.com/public-docs/index.html`
- Official OpenAPI document: `https://docs.rozetkapay.com/openapi.json`
- Local snapshot used by this repository: `docs/openapi.json`

## Path Coverage and Operation Parity Are Different Metrics

An SDK method can call the right **path** and still call the wrong **operation** — a different HTTP
verb, a body the operation does not declare, or a response shape the operation does not return. Path
coverage therefore does not imply operation parity, and the two are reported separately below.

### Pinned repository snapshot (`docs/openapi.json`)

- SHA-256: `98a9cf2a74b7df6edcaa17872d63f6bc9de96d77ca85a8adfb6a91af05c8e67a`
- Observed: `2026-07-25`
- Paths: `59`
- Operations: `67`
- Path coverage: `59/59`
- Operation coverage: a typed SDK method exists for each of the pinned `67` operations.
- Additional legacy compatibility routes in SDK: `25`

EXP-354 refreshed the snapshot from the `49`-path / `57`-operation document to the current one. The
snapshot is now byte-identical to the live official document, so the two views that used to be reported
separately have collapsed into one. Identity is asserted, not assumed:
`tests/SYT.RozetkaPay.Tests/OpenApi59OperationTests.cs` hashes the committed file and fails if it is not
exactly the document above.

### What "coverage" does and does not claim

The claim is about the **pinned document**: for every operation it declares, the SDK has a typed method
that sends the declared verb, request target, and body shape, and deserializes the declared response.
That is proven by the wire-level tests listed under each section below, and — since EXP-337 — by an
executable row per operation (see [Deterministic 67/67 Coverage](#deterministic-6767-coverage-exp-337)).

It is **not** a claim that a live sandbox has answered all `67` operations, and it never will be: most
published operations create, confirm, cancel, refund, or pay out real money. Calling all of them against
a shared environment would leave provider-side financial state behind, so the SDK deliberately does not
do it. This SDK therefore does **not** claim live, provider-verified `67/67` parity.

## New Operations (EXP-354)

Ten operations the previous snapshot did not contain. All ten are additive: no existing signature, route,
verb, body, or response type changed.

| operationId | Official verb and path | Service | Method | Request type | Response type | Auth | Transport notes |
|---|---|---|---|---|---|---|---|
| `UpdateSubscriptionPaymentMethod` | `PATCH /api/subscriptions/v1/subscriptions/{subscription_id}/payment-method` | `ISubscriptionService` | `UpdatePaymentMethodAsync(subscriptionId, request, ct)` | `UpdateSubscriptionPaymentMethodRequest` | `UpdateSubscriptionPaymentMethodResponse` | yes | `subscription_id` escaped once as one path segment; static log label; optional `X-CUSTOMER-AUTH` honoured |
| `createInStorePayment` | `POST /api/in-store-payments/v1/create` | `IInStorePaymentService` | `CreateAsync(request, ct)` | `InStorePaymentCreateRequest` | `InStorePaymentCreateResponse` | yes | JSON body; `currency` is the literal `"980"`; amounts are exact text |
| `confirmInStorePayment` | `POST /api/in-store-payments/v1/confirm` | `IInStorePaymentService` | `ConfirmAsync(request, ct)` | `InStorePaymentConfirmRequest` | `InStorePaymentConfirmResponse` | yes | JSON body carrying cardholder data; never logged |
| `refundInStorePayment` | `POST /api/in-store-payments/v1/refund` | `IInStorePaymentService` | `RefundAsync(request, ct)` | `InStorePaymentRefundRequest` | `InStorePaymentRefundResponse` | yes | JSON body carrying cardholder data; never logged |
| `getInStorePaymentInfo` | `POST /api/in-store-payments/v1/info` | `IInStorePaymentService` | `GetInfoAsync(externalId, ct)` | none | `InStorePaymentInfoResponse` | yes | **POST with no request content at all**; `external_id` query value escaped once |
| `feeDetails` | `GET /api/partners/v1/fee-details` | `IPartnerService` | `GetFeeDetailsAsync(ct)` / `(merchantProjectId, ct)` | none | `PartnerFeeDetailsResponse` | yes | no-argument overload sends no query string |
| `merchantStatus` | `GET /api/partners/v1/merchant-status` | `IPartnerService` | `GetMerchantStatusAsync(ct)` / `(options, ct)` | none | `MerchantStatusResponse` | yes | fixed order `merchant_project_id`, `merchant_entity_id` |
| `transactionDetails` | `GET /api/partners/v1/transaction-details` | `IPartnerService` | `GetTransactionDetailsAsync(merchantEntityId, ct)` / `(merchantEntityId, options, ct)` | none | `PartnerTransactionDetailsListResponse` | yes | required `merchant_entity_id` first, then `merchant_order_id`, `unified_external_id` |
| `createPaymentInstructions` | `POST /api/payment-instructions/v1/new` | `IPaymentInstructionService` | `CreateAsync(request, ct)` | `CreatePaymentInstructionsRequest` | `PaymentInstructionsResult` | yes | JSON body; enum tokens `cardpay`/`ppay` and `auth`/`purchase` pinned explicitly |
| `declinePaymentInstruction` | `GET /api/payment-instructions/v1/decline` | `IPaymentInstructionService` | `DeclineAsync(projectId, paymentInstructionId, ct)` | none | `PaymentInstructionDeclineResult` | **no** (`security: []`) | separate credential-free client; `AllowAutoRedirect = false`; `302` is success; `Location` returned unfetched |

Coverage: `SubscriptionPaymentMethodUpdateTests`, `InStorePaymentServiceTests`, `PartnerServiceTests`,
`PaymentInstructionServiceTests`, `OpenApi59OperationTests`.

### Unique callback operationIds

`/api/alternative-payments/v1/callback/resend` and `/api/payparts/v1/callback/resend` previously shared
the operationId `resendCallback`. The refreshed document gives them
`resendAlternativePaymentCallback` and `resendPayPartsCallback`. This is a documentation change only —
both are unchanged on the wire, and the corresponding SDK methods were **not** renamed or rewired. No
non-empty operationId in the pinned document is duplicated any more, which
`OpenApi59OperationTests` asserts.

### `declinePaymentInstruction`: no authentication, no redirect following

This operation is a security boundary, not a convenience detail.

- **Unauthenticated.** The official document declares `security: []`. The SDK attaches no
  `Authorization`, `Proxy-Authorization`, `X-ON-BEHALF-OF`, or `X-CUSTOMER-AUTH` header, even when the
  configuration supplies them.
- **A separate client, not a flag.** `HttpClient` has no per-request redirect switch, so the guarantee
  lives in a second client whose primary handler sets `AllowAutoRedirect = false`. Direct construction
  builds and owns that client; `AddRozetkaPay` configures a dedicated named client
  (`RozetkaPay.PaymentInstructions.Decline`) instead. Platform TLS validation is untouched in both paths:
  no certificate callback is installed and no check is relaxed.
- **The redirect is not followed and the target is not fetched.** The `Location` header is the result,
  returned as `PaymentInstructionDeclineResult`. Under the default disabled `RetryPolicy` a decline is a
  single request; when a retry policy is enabled the SDK may repeat the **same** decline request for the
  conditions it already supports, and never switches client, route, verb, or authentication mode. A
  successful `302` is never repeated.
- **The caller owns any navigation.** `Location` is provider-controlled input. Redirecting a browser to it
  is the normal use; fetching it server-side without validating scheme and host is a request-forgery
  sink. The SDK never does so on the caller's behalf.
- **`302` is success.** It never enters error mapping or retry handling. A `302` without a usable
  `Location` — absent, blank, or unparseable — throws `RozetkaPayException` with a static message that
  repeats neither the header value nor either identifier. Any other non-success status maps through the
  same status-to-exception table as every other operation.
- **Nothing sensitive is logged.** Only the static route `/api/payment-instructions/v1/decline` reaches a
  log sink — never `project_id`, `payment_instruction_id`, or `Location`.

A non-provider loopback test proves the transport end to end: the SDK sends one request to a local server
answering `302` towards a second local server, the second server receives nothing, and the recorded
request carries no credential header.

### Request-target safety carried over from EXP-353

EXP-353 established the encoding discipline for caller-supplied values, and every EXP-354 operation
follows it rather than re-deriving it:

- **Path segments** use `RequestTargetEncoding.EscapePathSegment`, which encodes exactly once and rejects
  the identifiers `.` and `..` with `ArgumentException` before any request is sent — `System.Uri` removes
  exact dot segments while building the request, so sending them would silently address a different
  endpoint. `UpdateSubscriptionPaymentMethod` is the only new operation with a path parameter, and it
  inherits both behaviours.
- **Query values** are escaped once with `Uri.EscapeDataString` at their own insertion point. A space
  becomes `%20`, never `+`; a literal `%` becomes `%25`, so a pre-encoded-looking value such as
  `already%2Fencoded` is sent as `already%252Fencoded`.
- **`null` omits, empty sends.** An optional parameter is left out only when it is `null`; an empty string
  is sent as an empty value for the provider to validate.
- **Parameter order is fixed by the SDK**, so two identical calls always produce the same request target.
- **Caller input never reaches a log label — for these ten operations.** Each one whose request target
  carries an identifier logs a static route template through a transport helper that takes a separate log
  label.

`PathSegmentEncodingTests` and `QueryParameterEscapingTests` still pass unchanged, and each new service's
tests assert the handler-observed `PathAndQuery` rather than the string the service built.

### Logging scope: what EXP-354 does and does not change

> **Superseded for the service-logging half.** The two subsections below describe EXP-354's own scope and are
> kept as the historical record of that ticket. The legacy gap they document — pre-existing operations logging
> their real request target, and `PaymentService.ConfirmP2PAsync` logging the external ID and the amount — was
> subsequently closed by **EXP-359**, which made the static-label contract SDK-wide and made the no-label
> `BaseService` overloads fail closed to `[redacted]`. For current behaviour see
> [`LOGGING_AUDIT.md`](LOGGING_AUDIT.md). The factory-logging removal described here is unchanged.

Two separate mechanisms produce HTTP log output, and only one of them is changed SDK-wide.

**Changed for every operation.** `AddRozetkaPay` now calls `RemoveAllLoggers()` on both named clients, so the
built-in `IHttpClientFactory` handler logging under `System.Net.Http.HttpClient.RozetkaPay.*` is not emitted
at all. That logging wrote the request URI — and while `Microsoft.Extensions.Http` 9.0.5 redacts the whole
query to `?*`, it does **not** redact path segments, so `subscription_id` reached `LogicalHandler` and
`ClientHandler` verbatim at Information level. Its header logging is redacted in the rendered message only:
the structured state of those entries carried the real `Authorization` and `X-CUSTOMER-AUTH` values at Trace
level. Neither is configurable, so the loggers are removed outright.

**Not changed, and not audited.** The SDK's own service logging for pre-existing operations. EXP-354 makes
**no** identifier- or content-safety claim about it, because changing it would mean touching operations
outside this ticket's scope. In the current code that logging includes:

- the **real request target** for most operations, with a caller identifier embedded in several routes —
  `/api/customers/v1/{customerId}/cards/{cardId}`, `/api/alternative-payments/v1/operation/{externalId}`,
  `/api/payparts/v1/operation/{operationId}`, and the query strings of list operations. The two-argument
  `BaseService` helpers pass the endpoint as its own log label, which is exactly the pre-EXP-354 behaviour;
- **method-specific values** in at least one place: `PaymentService.ConfirmP2PAsync` logs the external ID and
  the amount.

EXP-354 also listed the **transport exception message** in the shared retry warning as unaudited text. EXP-356
removed it: the retry warning now reports only the retry number, the configured budget, the failure category as
an exception type name, the HTTP status when the failure came from a response, and the computed delay. See
**Retry Behaviour** below.

### What EXP-354 does claim

Scoped to the ten operations in the table above, and limited to what the tests actually measure under the
default disabled retry policy. Two layers of coverage, and it takes both:

| Layer | Tests | What it measures |
|---|---|---|
| Per-operation service logging | `SubscriptionPaymentMethodUpdateTests` (subscription update), `InStorePaymentServiceTests.EveryOperation_ShouldLogTheStaticRouteOnly` (all four in-store), `PartnerServiceTests.EveryOperation_ShouldLogTheStaticRouteOnly` (all three partner), `PaymentInstructionServiceTests.Create_ShouldLogTheStaticRouteAndNothingFromTheRequestOrResponse` and `…Decline_ShouldLogNeitherIdentifierNorLocation` | Each operation's own log statements, through a recording logger, with hostile markers in the request and the response |
| Whole pipeline through DI | `Exp354FactoryLoggingTests` | A real `AddRozetkaPay` driven through a capturing `ILoggerProvider` that inspects every category, rendered message, structured value and scope — this is what catches logging the SDK does not write itself |

All ten operations are covered by the first layer; the second layer is what proves nothing else in the
pipeline re-introduces a leak. With those in place:

- each of those operations logs a **static route template** and the response status;
- their captured logging contains **no** caller identifier (`subscription_id`, in-store `external_id`, the
  partner query identifiers, decline `project_id` and `payment_instruction_id`), **no** credential
  (`Authorization`, `X-ON-BEHALF-OF`, `X-CUSTOMER-AUTH`, the configured password), **no** request or response
  body, and **no** `RozetkaPayApiError.RawBody`;
- the decline operation additionally does not log the `Location` it returns.

Nothing beyond those ten operations is asserted **by EXP-354**. EXP-359 later extended the same guarantee to
every operation and added `LegacyLoggingRedactionTests`; see [`LOGGING_AUDIT.md`](LOGGING_AUDIT.md).

### Historical partner DTOs are left intact

`Models.Merchants.PartnersFeeDetails`, `Models.Merchants.PartnersTransactionDetails`,
`Models.Merchants.FeeDetailsResponse`, `Models.Merchants.TransactionDetailsListResponse`, and the
same-named types in `Models.Common` describe an older layout that does not match the official partner
responses. They remain public and unchanged — removing them would break compiled consumers — but no new
operation returns them. The correctly shaped results live in `Models.Partners`.
`merchantStatus` deliberately reuses `Models.Merchants.MerchantStatusResponse`, whose shape already
matches the official response exactly.

Likewise, the historical `Models.Subscriptions.SubscriptionPaymentMethod` is not repurposed: the new
operation uses `SubscriptionPaymentMethodUpdate`.

## Corrected Operation Mismatches (EXP-355)

Each row is a published operation the SDK previously reached only through a legacy verb, path, body,
or response shape. Both members exist; the legacy one is obsolete and unchanged on the wire.

| operationId | Official verb and path | Canonical SDK method | Legacy SDK method | Fallback policy |
|---|---|---|---|---|
| `deleteCustomerPayment` | `DELETE /api/customers/v1/wallet` (optional `external_id`, JSON body, `DeleteCustomerPaymentResult`) | `ICustomerService.DeleteCustomerPaymentAsync(request, ct)` and `(externalId, request, ct)` | `ICustomerService.DeletePaymentFromWalletAsync(customerId, cardId, ct)` — `DELETE /api/customers/v1/{customerId}/cards/{cardId}`, no body, `DeleteCardFromWalletResponse` | none |
| `getSubscriptions` | `GET /api/subscriptions/v1/subscriptions` (optional `external_id`, root JSON array of `Subscription`) | `ISubscriptionService.GetSubscriptionsAsync(ct)` and `(externalId, ct)` | `ISubscriptionService.GetCustomerSubscriptionsAsync(customerId, ct)` — `GET /api/subscriptions/v1/subscriptions/customer/{customerId}`, `CustomerSubscriptionsResponse` wrapper | none |
| `CancelCustomerSubscription` | `DELETE /api/subscriptions/v1/subscriptions/{subscription_id}/cancel` (optional `external_id`, optional lowercase `refund`, no body, `DefaultResponse`) | `ISubscriptionService.CancelCustomerSubscriptionAsync(subscriptionId, ct)` and `(subscriptionId, options, ct)` | `ISubscriptionService.CancelAsync(subscriptionId, request, ct)` — `POST` to the same path with an `external_id`/`reason`/`immediate` body, returns `Task` | none |

### Why there is no fallback

The canonical and legacy members do not describe the same call:

- the wallet responses are different types (`DeleteCustomerPaymentResult` vs `DeleteCardFromWalletResponse`);
- the list responses are different types (root array vs wrapper object);
- the cancel request semantics differ, and the legacy `reason` and `immediate` fields have no honest
  mapping onto the official `refund` option — guessing `immediate -> refund` would silently change what
  the caller asked for.

No canonical operation therefore ever switches to a legacy route or verb. A canonical `404` makes
**exactly one** HTTP request and throws `RozetkaPayNotFoundException`; `400`, `401`, `403`, `412`, `429`
and `500` keep their existing exception mapping. A fallback would hide an operation-parity error behind
a response that looks successful.

Retrying is a separate concern. The default `RetryPolicy` is disabled, so by default a canonical call is a
single request. When a retry policy is enabled, the SDK may repeat the **same** canonical request target for
the conditions the policy declares — see **Retry Behaviour** below. Such a repeat is always the same operation
against the same target; it is never a different route, verb, or body. A `404` is not in the default retriable
set, and a caller who adds it still gets a repeat of the same canonical request, never a legacy attempt.

### Why the old members are only obsolete

Removing or redirecting them would break compiled consumers. Every pre-existing signature keeps its name,
parameters, return type and wire behaviour; each carries an `[Obsolete]` warning naming its replacement.

## Response Shape Compatibility

`SubscriptionList` keeps its public shape — the class, the `Subscriptions` property, its type and its
base type are unchanged — while an internal `JsonConverter<SubscriptionList>` maps the official root
array onto it. Reads accept both the official array and the historical `{ "subscriptions": [...] }`
wrapper; writes always emit the official root array. Null and empty stay distinct: an official `[]`
yields an empty list, a wrapper carrying `"subscriptions": null` yields `null`, and an absent list is
normalized to `[]` when serializing, because the official schema has no spelling for "no array at all".

## Retry Behaviour (EXP-356)

`RetryPolicy.RetriableStatusCodes` is a public setting, and the runtime now honours exactly the set it
declares. Before EXP-356 the loop had a status-specific branch for `429` only, so the other five default
statuses — `500`, `502`, `503`, `504`, `408` — were configured as retriable but escaped on the first attempt,
and a custom set had no effect on any status but `429`. Statements in this document that described retrying as
covering "transport-level failures and `429`" described that gap and are superseded by this section.

**No public API changed.** `RetryPolicy` keeps every property and factory, `MaxRetryAttempts` keeps its
meaning, and every public exception constructor is unchanged. The default policy is still disabled.

| Contract | Behaviour |
|---|---|
| Attempts | Exactly `1 + MaxRetryAttempts` requests. `Enabled = false` or `MaxRetryAttempts = 0` sends one. |
| Status decision | Read from the response the SDK received (`RozetkaPayException.ApiError.StatusCode`) against `RetriableStatusCodes` — never from an exception type, message, or provider code. |
| Default set | Exactly `408`, `429`, `500`, `502`, `503`, `504`. |
| Custom set | Honoured as configured, additions and removals alike; an empty set retries no status. |
| Transport failures | `HttpRequestException`, `SocketException`, and timeout-like `TaskCanceledException` — exactly the categories `RetryPolicy.ShouldRetry(Exception)` publishes. The only wrapper unwrapped is the SDK's own: a `RozetkaPayException` over one of those categories. An exception the SDK did not raise is never made retriable by its inner exception. |
| Repeat shape | Same verb, target, body bytes, content type, and authentication mode; a fresh request object per attempt. |
| Caller cancellation | Never retried; no delay is scheduled and the `OperationCanceledException` propagates unwrapped. A token cancelled **before** the call is rejected before anything is dispatched at all — see [Pre-Dispatch Cancellation Contract (EXP-357)](#pre-dispatch-cancellation-contract-exp-357). |
| `429` `Retry-After` | Delta-seconds or HTTP-date replaces the backoff, zero/past means immediate, positive values are capped by `MaxDelay`, absent or unparseable falls back to the configured backoff. Ignored on other statuses. |
| Exhaustion | The last attempt's own exception, with its `RozetkaPayApiError` (final status, code, request identifier, raw body). No wrapper, and no merged evidence from earlier attempts. |
| Resources | Every attempt disposes its own request, request body, response, and response content — on success, on a retried failure, on exhaustion, and on cancellation. |
| Retry log | One `Warning` per retry: retry number, budget, failure category, HTTP status when known, delay in ms. No exception message, body, provider text, request target, or credential. |

Two incidental corrections come with the centralized predicate: a `SocketException` raised directly by a
handler is now retried, which is what `RetryPolicy.ShouldRetry(Exception)` has always published; and a
negative `MaxRetryAttempts` — which options validation already rejects — now sends one attempt instead of
throwing `Request failed for unknown reason` without calling the operation at all.

Retrying a **mutating** operation can produce a second provider-side operation: the attempt that appeared to
fail may have been accepted. The SDK does not claim exactly-once delivery and cannot make a repeat safe on its
own. Callers enabling retries for mutating calls must supply a stable `external_id` / idempotency value per
business intent and reconcile by it.

Verified by `RetryStatusCodeContractTests` (`77` cases per target framework): the six default statuses through
seven transport helpers plus the anonymous decline redirect, attempt-count arithmetic, disabled and custom
sets, exhaustion evidence, transport compatibility — including the negative case that an arbitrary exception
wrapping an `HttpRequestException` is **not** retried — caller cancellation, the `Retry-After` matrix,
per-attempt disposal, and log-leak assertions with hostile markers. The suite never touches a socket — its
base address is in the reserved `.invalid` TLD and the transport never forwards — and it contains no sleep and
no timing-threshold assertion: every wait-sensitive case is settled by cancelling from the retry-warning
callback, immediately before the delay is awaited.

## Pre-Dispatch Cancellation Contract (EXP-357)

An already-cancelled `CancellationToken` is now rejected by the SDK itself, in one place, for every transport
helper — before helper logging, before JSON serialization of the caller's body, before any retry bookkeeping,
before an `HttpRequestMessage` is allocated, and before `HttpClient` or an `HttpMessageHandler` is invoked.

Before EXP-357 only the two DELETE paths and the payment-instruction decline carried an explicit guard. Every
other helper entered the shared retry executor and left the outcome to the runtime's own pre-dispatch check,
which is not a contract: it fires at different points on `net9.0` and `net10.0` and differs per verb. Measured
on this repository's exact base commit, an already-cancelled token produced these outcomes:

| Helper | `net9.0` before | `net10.0` before |
|---|---|---|
| `GET`, `GET` fallback, bodiless `POST` | request dispatched, call **succeeded** | request dispatched, call **succeeded** |
| `POST` (JSON), `POST` accepting `204`, `PATCH`, `POST`/`POST`-`204` fallbacks | cancelled, but only **after** the request log was written | request dispatched, call **succeeded** |
| `DELETE` with a JSON body | cancelled, but only **after** the caller's body was serialized | cancelled, but only **after** the caller's body was serialized |
| Bodiless `DELETE`, decline | correct (explicit guard since EXP-355 / EXP-354) | correct |
| Shared retry executor, called directly | attempt delegate invoked | attempt delegate invoked |

**No public API changed.** No interface, method, overload, optional cancellation-token default, DTO, exception
type or constructor, `RetryPolicy` member, or DI registration was touched, and no route, verb, body, content
type, response mapping, or redirect behaviour changed for a token that is not cancelled.

| Contract | Behaviour |
|---|---|
| Where the check lives | First executable line of `BaseService.ExecuteWithRetryAsync`, before `Configuration.RetryPolicy` is read and before any retry counter exists; plus the top of every later loop iteration, before the attempt delegate |
| Coverage | Authenticated `GET`, JSON `POST`, `POST` accepting `204`/empty, JSON `PATCH`, bodiless `POST`, bodiless `DELETE`, JSON-body `DELETE`, the three `404` fallback wrappers, and the unauthenticated non-redirecting decline |
| Handler invocations | Exactly `0`, on both target frameworks |
| Helper logging | None. A pre-cancelled call writes no log entry at all, so it cannot leak a target, a body, or a credential it never used |
| Serialization | None. The eager serialization in the JSON-body `DELETE` happens outside the attempt, so that helper carries its own guard **before** `JsonSerializer.Serialize` |
| Token identity | `ThrowIfCancellationRequested`, so the `OperationCanceledException` carries the caller's exact token. Never a hand-built exception and never a tokenless one |
| Throw timing | The public methods stay `async`; the exception surfaces on `await`. Synchronous throw timing is **not** claimed and was not changed |
| Fallback wrappers | The check is the first statement of each `catch (RozetkaPayNotFoundException)`, so cancellation after a primary `404` prevents the fallback request **and** the "falling back" log. `OperationCanceledException` is never caught |
| Decline | Keeps its own guard after argument validation and before the request URI is built, because it does work before reaching the executor |
| Retry policy | Enabled and disabled produce identical semantics; the check precedes the policy read |
| Mid-flight cancellation | Unchanged: one attempt, no retry, no fallback, caller's token preserved, and the attempt still disposes its request, body, response, and content |
| Timeouts | Unchanged: a timeout-like `TaskCanceledException` with a live caller token is still a retriable transport failure and is never reported as caller cancellation |

Verified by `PreDispatchCancellationContractTests` (`75` cases per target framework). Because a handler counter
alone cannot prove the contract — a framework that rejects the token inside `HttpClient` would mask a helper
that had already logged and serialized — every pre-cancelled case asserts four independent tripwires: handler
invocations, captured log entries, a request body whose property getter counts serializer accesses, and, for the
shared executor, the attempt delegate itself. All must read zero. The matrix runs every helper row with the
retry policy disabled and enabled, and each row is also run with a **live** token, so a broken tripwire fails
the suite instead of silently passing it. The suite never touches a socket — its base address is in the reserved
`.invalid` TLD and the transport never forwards — and contains no sleep and no timing-threshold assertion: the
"cancelled after the primary `404`" cases are settled by cancelling from the API-error log callback, and the
mid-flight cases by cancelling from inside the handler.

## Deterministic 67/67 Coverage (EXP-337)

EXP-337 adds an executable contract row for **every one of the `67` operations**, plus real HTTP-boundary
tests for outbound authentication and inbound webhook handling. Nothing in this section requires the
network, and all of it runs in ordinary CI on both `net9.0` and `net10.0`.

Read "67/67" precisely: **67 SDK operations produce exactly the 67 requests the pinned document
declares.** It is not, and does not become, a statement that RozetkaPay answered 67 calls.

### Layer A — operation contract, always in CI

`tests/SYT.RozetkaPay.Tests/TestInfrastructure/OpenApiOperationManifest.cs` is a hand-written table with
one canonical row per published operation. `OpenApiOperationContractTests` compares that table against the
pinned `openapi.json` as exact sets on `(HTTP method, path template, operationId)`, then invokes every row's
canonical SDK method over a recording transport and asserts the request it produced.

| Property | How it is proven |
|---|---|
| No operation missing, added, renamed, duplicated, or moved to another verb | Both sets compared on the full identity; the guard has its own drift meta-tests, so a comparison that silently matched the manifest against itself would fail |
| Correct verb and concrete request target | Literal expectation per row; expected values are never produced by the production URL helper |
| Percent-encoding applied exactly once, at the right insertion point | Caller values carry `space + / & = ? # %` and Cyrillic text; the encoded form is a separate literal |
| Correct body policy | Cross-checked against `requestBody` in the pinned document, then asserted on the wire: `application/json; charset=utf-8`, or no content object at all |
| Correct authentication policy | Cross-checked against operation-level `security` in the document; `Basic` decoded and compared as UTF-8 `login:password` |
| The one anonymous operation stays anonymous | `declinePaymentInstruction` carries no `Authorization`, `Proxy-Authorization`, `X-ON-BEHALF-OF`, or `X-CUSTOMER-AUTH`, sends no content, and returns the `302` `Location` without fetching it |
| Optional headers sent only when configured | Every row re-run with `OnBehalfOf` and `CustomerAuth` unset |
| Cancellation reaches the transport | Every row cancelled mid-flight from inside the handler |
| No call can reach the network | Base address is in the reserved `.invalid` TLD and the transport never forwards; every row asserts the host it observed |

The controlled transport answers a deterministic `400`. That exercises the SDK error path without
duplicating 67 success schemas and — unlike a `404` — never triggers the legacy-route fallbacks several
services still carry, so "exactly one request per operation" stays a real assertion.

Rows call the **canonical** member, never a legacy one: `DeleteCustomerPaymentAsync`,
`GetSubscriptionsAsync`, `CancelCustomerSubscriptionAsync`, `RequestPayoutAsync`, and the two-argument
`GetOperationInfoAsync` overloads. The `25` legacy compatibility routes are not counted as coverage.

### Layer B — real HTTP boundaries, always in CI

`HttpBoundaryIntegrationTests` and `WebhookHttpBoundaryTests` run the SDK against a real ASP.NET Core /
Kestrel server over a real socket, bound to `127.0.0.1` on an ephemeral port. They prove what a stubbed
handler cannot: what actually goes on the wire, and what an endpoint actually receives.

- Outbound: `Basic` decodes as UTF-8 to exactly the configured non-ASCII placeholders with exactly one
  separating colon; `X-ON-BEHALF-OF`, `X-CUSTOMER-AUTH`, and the user agent arrive verbatim; no credential
  appears anywhere in the request target; a typed response comes back.
- Anonymous decline: no credential-bearing header arrives, query values are escaped once, the `Location`
  of a `302` is returned, and a **reachable** redirect target records zero requests.
- Ownership: the self-owning `PaymentInstructionService` constructor really releases its decline client on
  disposal, and never touches a caller-supplied one.
- Inbound webhook: raw bytes read once and verified **before** deserialization or any side effect; a
  missing, malformed, mismatched, or duplicated signature header fails closed with `400` and a static
  reason; a one-byte mutation and a semantically identical re-serialization are both rejected.

Expected webhook signatures come from the independent Python reference vectors already pinned by
`WebhookSignatureVerifierTests`, never from the verifier under test.

### Layer C — live sandbox smoke, opt-in only

One test, read-only, off by default: `SandboxSmokeTests.ValidateMerchantKeys_ShouldAnswerOverTheLiveSandbox`
calls only `validateMerchantKeys` (`GET /api/merchants/v1/me`), the operation that exists to be called this
way. It resolves the SDK through the supported DI/options route with
`Environment = RozetkaPayEnvironment.Sandbox`, asserts the base URL is the official sandbox constant, uses a
bounded timeout, and never retries or falls back to production.

Without **both** environment variables it is skipped with the reason
`Requires ROZETKAPAY_SANDBOX_LOGIN and ROZETKAPAY_SANDBOX_PASSWORD. No network call was made.` and makes no
network call. Absent credentials are never a pass and never fail an ordinary build; the reason never says
which variable is missing and never contains a value.

```bash
# Opt in explicitly. Values are placeholders - never commit real ones, and never paste them into CI logs.
ROZETKAPAY_SANDBOX_LOGIN='<login>' \
ROZETKAPAY_SANDBOX_PASSWORD='<password>' \
dotnet test tests/SYT.RozetkaPay.Tests/SYT.RozetkaPay.Tests.csproj -c Release --filter 'Category=Sandbox'

# Everything deterministic, explicitly excluding the live check.
dotnet test tests/SYT.RozetkaPay.Tests/SYT.RozetkaPay.Tests.csproj -c Release --filter 'Category!=Sandbox'
```

There is no scheduled workflow and no CI secret for the sandbox. A workflow that reported green because
secrets were absent would be a false claim of live verification, so it does not exist.

## Last Verification

- Date: `2026-07-25`
- Result: deterministic contract coverage for all `67` pinned operations, executed per operation on both
  target frameworks; real Kestrel HTTP-boundary coverage for outbound authentication, the anonymous
  decline/redirect, and the inbound webhook signature pipeline; the full retry contract of EXP-356 and the
  full pre-dispatch cancellation contract of EXP-357 executed against the production retry loop and the
  production transport helpers over a non-forwarding transport; one opt-in read-only live sandbox smoke
  test, skipped in this run.
- Snapshot SHA-256: `98a9cf2a74b7df6edcaa17872d63f6bc9de96d77ca85a8adfb6a91af05c8e67a` (unchanged; none of
  EXP-337, EXP-356, or EXP-357 touches `docs/openapi.json`)
- Snapshot path count: `59`; snapshot operation count: `67`
- SDK service coverage for snapshot OpenAPI paths: `59/59`
- Deterministic operation contract coverage: `67/67`, asserted as an exact set against the pinned document
- Retry contract coverage: `77` cases per target framework — six default statuses, seven transport helpers,
  the anonymous decline redirect, attempt-count arithmetic, disabled and custom sets, exhaustion evidence,
  transport compatibility including the arbitrary-wrapper negative case, caller cancellation, the
  `Retry-After` matrix, per-attempt disposal, and log-leak assertions
- Pre-dispatch cancellation coverage: `75` cases per target framework — ten transport-helper rows (seven
  direct helpers plus the three `404` fallback wrappers) run with the retry policy disabled and enabled, the
  shared retry executor asserted directly, the unauthenticated decline path, cancellation after a primary
  `404`, mid-flight cancellation per direct helper, the timeout-with-a-live-token negative case, and
  live-token controls proving every tripwire can fire
- Test result: `net9.0` — `1394` passed, `1` skipped, `0` failed; `net10.0` — `1394` passed, `1` skipped,
  `0` failed. The single skip is the live sandbox smoke test, skipped because no sandbox credentials were
  supplied.
- Build: `Release` with `-warnaserror` — `0` warnings, `0` errors
- Verified by: `OpenApiOperationContractTests`, `HttpBoundaryIntegrationTests`, `WebhookHttpBoundaryTests`,
  `SandboxSkipBehaviorTests`, `OpenApi59OperationTests`, `OperationParityTests`,
  `SubscriptionPaymentMethodUpdateTests`, `InStorePaymentServiceTests`, `PartnerServiceTests`,
  `PaymentInstructionServiceTests`, `PathSegmentEncodingTests`, `QueryParameterEscapingTests`,
  `WebhookSignatureVerifierTests`, `PublicInterfacesTests`, `PublicInterfaceRegistrationTests`,
  `Exp354DisposalTests`, `RetryStatusCodeContractTests`, `PreDispatchCancellationContractTests`
- **Not** verified: that a live RozetkaPay environment answers all `67` operations. No mutating operation
  was called against any live environment, and none will be. The live check is exactly one read-only
  merchant identity call, and it did not run here.

## Known Runtime Inconsistency (Observed in Integrations)

- Some API responses return numeric values as JSON strings (for example, `"100.00"` or `"2"`) instead of JSON numbers (`100.00`, `2`), despite OpenAPI declaring numeric types.
- This behavior was observed in integration testing and reported to RozetkaPay.
- As of `2026-02-28`, this is still reproducible on selected endpoints.
- SDK mitigation:
  - flexible decimal converters for `decimal` / `decimal?`
  - flexible integer converters for `int` / `int?` / `long` / `long?`
  - global `JsonNumberHandling.AllowReadingFromString` enabled in serializer options
  - converters are read-compatible with both formats, so SDK remains stable before and after potential API-side normalization.
