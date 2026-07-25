# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Maintainers move entries out of `Unreleased` into a new versioned section
immediately before tagging a release (see the release process in `README.md`).

## [Unreleased]

### Added
- Executable contract coverage for **all `67` operations** of the pinned OpenAPI snapshot (EXP-337).
  `tests/SYT.RozetkaPay.Tests/TestInfrastructure/OpenApiOperationManifest.cs` holds one hand-written
  canonical row per published operation; `OpenApiOperationContractTests` compares that table with
  `docs/openapi.json` as exact sets on `(HTTP method, path template, operationId)` — so an operation
  added, removed, renamed, duplicated, or moved to another verb fails the build — and then invokes every
  row's canonical SDK method and asserts the request it produced. Per row: exact verb and concrete
  request target against a literal, single-pass percent-encoding of hostile caller values, body policy
  cross-checked against the document's `requestBody`, content type asserted on the wire, and authentication
  policy cross-checked against the document's operation-level `security`; `Basic` is decoded and compared as UTF-8
  `login:password`, optional headers present only when configured, and the caller's cancellation token
  observed at the transport. The suite cannot reach the network: the base address is in the reserved
  `.invalid` TLD and the transport never forwards. Rows call canonical members only — the `25` legacy
  compatibility routes are not counted as coverage. Test-only; no production code changed.
- Real HTTP-boundary coverage against ASP.NET Core / Kestrel on loopback (EXP-337), through a test-only
  `Microsoft.AspNetCore.App` framework reference — no new NuGet dependency, and no change to the
  package's published dependency set. `HttpBoundaryIntegrationTests` proves what an endpoint actually
  receives: `Basic` decoding as UTF-8 to exactly the configured non-ASCII placeholders with one
  separating colon, `X-ON-BEHALF-OF` / `X-CUSTOMER-AUTH` / user agent arriving verbatim, no credential
  anywhere in the request target, and — for `declinePaymentInstruction` — no credential-bearing header at
  all, a returned `Location`, and a reachable redirect target that records zero requests.
  `WebhookHttpBoundaryTests` drives the documented callback pipeline over real HTTP: raw bytes verified
  before deserialization or any side effect, and fail-closed `400` for a missing, malformed, mismatched,
  or duplicated signature header, a one-byte body mutation, and a semantically identical re-serialization.
  Expected signatures come from the independent reference vectors already pinned by
  `WebhookSignatureVerifierTests`, never from the verifier under test.
- Opt-in live sandbox smoke test (EXP-337): `SandboxSmokeTests` calls only `validateMerchantKeys`
  (`GET /api/merchants/v1/me`) through the supported DI/options route with
  `Environment = RozetkaPayEnvironment.Sandbox`, a bounded timeout, no retry, and no fallback to
  production. `SandboxFactAttribute` skips it unless both `ROZETKAPAY_SANDBOX_LOGIN` and
  `ROZETKAPAY_SANDBOX_PASSWORD` are set, with a reason that names the variables, states that no network
  call was made, and reveals neither which variable is missing nor any value. Absent credentials are
  never a silent pass and never fail an ordinary build. No mutating operation is ever called live, no
  scheduled workflow was added, and no CI secret was configured.
- Typed coverage for the ten operations the refreshed OpenAPI snapshot publishes and the previous one
  did not. All additive: no existing signature, route, verb, body, or response type changed.
- `ISubscriptionService.UpdatePaymentMethodAsync(subscriptionId, request, ct)` — official
  `UpdateSubscriptionPaymentMethod`:
  `PATCH /api/subscriptions/v1/subscriptions/{subscription_id}/payment-method`, with
  `UpdateSubscriptionPaymentMethodRequest`, `SubscriptionPaymentMethodUpdate`,
  `SubscriptionPaymentMethodUpdateType`, `SubscriptionRecurrentIdPaymentMethod`, and
  `UpdateSubscriptionPaymentMethodResponse` in `SYT.RozetkaPay.Models.Subscriptions`. The
  identifier is escaped once as one path segment and never logged; the configured `X-CUSTOMER-AUTH`
  header is honoured. The historical `SubscriptionPaymentMethod` is untouched, not repurposed.
- `IInStorePaymentService` / `InStorePaymentService` with the four official in-store (POS) operations:
  `CreateAsync`, `ConfirmAsync`, `RefundAsync` (`POST /api/in-store-payments/v1/{create,confirm,refund}`)
  and `GetInfoAsync` (`POST /api/in-store-payments/v1/info?external_id=…`). Request and response models
  live in `SYT.RozetkaPay.Models.InStorePayments`, including three distinct receipt types because the
  create, confirm, and refund receipts are three different official shapes. Amounts are strings in the
  smallest monetary unit and are carried verbatim; `InStorePaymentCurrency.Uah` serializes to the
  literal `"980"`.
- `IPartnerService` / `PartnerService` with the three official partner operations: `GetFeeDetailsAsync`,
  `GetMerchantStatusAsync`, and `GetTransactionDetailsAsync`, each with a no-query and an explicit-query
  overload. `PartnerFeeDetailsResponse`, `PartnerFeeDetails`, `PartnerTransactionDetailsListResponse`,
  `PartnerTransactionDetails`, `PartnerMerchantStatusOptions`, and `PartnerTransactionDetailsOptions`
  live in the new `SYT.RozetkaPay.Models.Partners` namespace. Merchant status reuses the existing
  `Models.Merchants.MerchantStatusResponse`, whose shape already matches the official response.
- `IPaymentInstructionService` / `PaymentInstructionService` with `CreateAsync`
  (`POST /api/payment-instructions/v1/new`) and `DeclineAsync`
  (`GET /api/payment-instructions/v1/decline`). `SYT.RozetkaPay.Models.PaymentInstructions` adds
  `CreatePaymentInstructionsRequest`, `PaymentInstructionPayer`, `PaymentInstructionOrder`,
  `PaymentInstructionsResult`, `PaymentInstruction`, `PaymentInstructionDeclineResult`,
  `PaymentInstructionProcessingType`, and `PaymentInstructionMethod`. Enum wire tokens
  (`cardpay`/`ppay`, `auth`/`purchase`) are pinned on the members instead of derived from the naming
  policy, which would emit `card_pay` and `p_pay`.
- `declinePaymentInstruction` is sent unauthenticated and its redirect is never followed. The official
  document declares it `security: []` and answers a bare `302` whose `Location` header is the result.
  Because `HttpClient` has no per-request redirect switch, the SDK uses a dedicated client whose primary
  handler sets `AllowAutoRedirect = false` and which carries no `Authorization`,
  `Proxy-Authorization`, `X-ON-BEHALF-OF`, or `X-CUSTOMER-AUTH` header. The SDK returns the `Location`
  without reading or fetching the target; deciding whether to navigate there — and validating it first —
  belongs to the caller. Platform TLS validation is unchanged: no certificate callback is installed.
- `IRozetkaPayClient` and `RozetkaPayClient` expose `InStorePayments`, `Partners`, and
  `PaymentInstructions`; `AddRozetkaPay` registers all three concrete services and interface aliases as
  scoped, resolving to the same instance, and adds one dedicated non-redirect named `HttpClient`
  (`RozetkaPay.PaymentInstructions.Decline`) sharing only the endpoint, timeout, and user agent.
- Test coverage for the above: `OpenApi59OperationTests`, `SubscriptionPaymentMethodUpdateTests`,
  `InStorePaymentServiceTests`, `PartnerServiceTests`, `PaymentInstructionServiceTests`, and
  `Exp354DisposalTests`, including a non-provider loopback test proving that the decline request carries
  no credential and that the redirect target receives nothing.
- Canonical members for three published operations that the SDK previously reached only through a
  legacy verb, path, body or response shape:
  - `ICustomerService.DeleteCustomerPaymentAsync(request, ct)` and
    `(externalId, request, ct)` — official `deleteCustomerPayment`:
    `DELETE /api/customers/v1/wallet` with an optional `external_id` query value, a JSON request body,
    and a typed `DeleteCustomerPaymentResult` response.
  - `ISubscriptionService.GetSubscriptionsAsync(ct)` and `(externalId, ct)` — official
    `getSubscriptions`: `GET /api/subscriptions/v1/subscriptions` with an optional `external_id` query
    value, reading the official root JSON array.
  - `ISubscriptionService.CancelCustomerSubscriptionAsync(subscriptionId, ct)` and
    `(subscriptionId, options, ct)` — official `CancelCustomerSubscription`:
    `DELETE /api/subscriptions/v1/subscriptions/{subscription_id}/cancel` with no request body and a
    typed `DefaultResponse`.
  The token-only overloads identify the customer through the configured `X-CUSTOMER-AUTH` header.
  Distinct names, not overloads of the old members, so no existing call becomes ambiguous.
- `CancelCustomerSubscriptionOptions` in `SYT.RozetkaPay.Models.Subscriptions`: the optional
  `external_id` and `refund` **query** parameters of the cancel operation. It is never serialized —
  the operation sends no body. `null` omits a parameter, an empty `ExternalId` is sent as
  `external_id=` for the provider to validate, `refund` is rendered lowercase and invariantly, and the
  query order is always `external_id` then `refund`.
- `RozetkaPayApiError` in `SYT.RozetkaPay.Exceptions` and `RozetkaPayException.ApiError`: structured
  details of a failed API call — HTTP status (`HttpStatusCode`), provider error code, request
  identifier, and the raw response body. Every exception raised from a non-success HTTP response
  carries one; manually constructed exceptions, transport failures, and responses the SDK cannot
  deserialize leave `ApiError` as `null`.
- The error code is exposed as a `string`, not as the `ResponseCode` enum, so a code the provider adds
  after this release survives unchanged instead of failing to deserialize or being mapped onto a wrong
  fallback value. A numeric code keeps its raw JSON text.
- The request identifier is resolved from the `X-Request-Id` response header, then `Request-Id`, then
  the payload `error_id`, then `error.error_id`. Header matching is case-insensitive and blank values
  are skipped.
- The raw response body is preserved verbatim, so a malformed, plain-text, or unmodelled payload stays
  inspectable and still yields the same status-specific exception instead of a parser error. The SDK
  never logs the raw body and never places it in `Exception.Message` or `Exception.ToString()`; the
  error log line carries only the HTTP status, the API code, and the request ID. Callers must treat the
  raw body as sensitive and scrub it before logging or storage.
- `RozetkaPayOptions` and `RozetkaPayEnvironment` in `SYT.RozetkaPay.Configuration`: typed
  settings bound from the `RozetkaPay` configuration section
  (`RozetkaPayOptions.SectionName`) and resolvable as `IOptions<RozetkaPayOptions>`.
- Sandbox/production switch: `Environment` selects the endpoint published by the official
  OpenAPI document — `RozetkaPayOptions.ProductionBaseUrl` (the default) or
  `RozetkaPayOptions.SandboxBaseUrl`. An explicit `BaseUrl` still overrides it.
- Validation through the options pipeline — DataAnnotations plus an
  `IValidateOptions<RozetkaPayOptions>` validator for the cross-field rules — checked with
  `ValidateOnStart()`, so a broken configuration throws `OptionsValidationException` while the
  host starts instead of during the first request. Failure messages name the configuration key
  and never contain credentials.
- `AddRozetkaPay(Action<RozetkaPayOptions>)` for configuring the SDK in code, without an
  `IConfiguration`.
- `IRozetkaPayWebhookSignatureVerifier` and `RozetkaPayWebhookSignatureVerifier` in
  `SYT.RozetkaPay.Security`, verifying the `X-ROZETKAPAY-SIGNATURE` header on incoming
  callbacks against the raw request body. Registered as a singleton by `AddRozetkaPay`.
  Missing, malformed, and incorrect signatures return `false` instead of throwing, and
  digests are compared in constant time.
- Tag-based [MinVer](https://github.com/adamralph/minver) versioning; the package
  version is derived from the `v*.*.*` release tag instead of a hardcoded value.
- Pull request build, test, and package verification (`Build & Test` workflow).
- Tag-triggered NuGet publishing and GitHub Releases (`Release NuGet` workflow).

### Changed
- The documented webhook receiver now requires **exactly one** `X-ROZETKAPAY-SIGNATURE` header value
  before verifying (EXP-337). The previous snippet used `FirstOrDefault()`, which silently picks one of
  several values and lets a sender append a header to choose which value is checked. `Verify` takes a
  single value and cannot see that a second one arrived, so this is the caller's check to make; the
  behaviour is now pinned by `WebhookHttpBoundaryTests`. Documentation only — no API change.
- README and `docs/API_COMPATIBILITY.md` now state precisely what is deterministic, what runs against a
  real local HTTP boundary, and what is live: `67/67` means 67 SDK operations produce the 67 requests the
  pinned document declares, verified without a network. The live sandbox check is one read-only merchant
  identity call, is opt-in, and is reported as skipped in CI because no sandbox secret is configured.
- The pinned OpenAPI snapshot `docs/openapi.json` is refreshed to the official document observed on
  `2026-07-25` — SHA-256 `98a9cf2a74b7df6edcaa17872d63f6bc9de96d77ca85a8adfb6a91af05c8e67a`, `59` paths,
  `67` operations, up from `49` paths and `57` operations. `OpenApi59OperationTests` hashes the committed
  file, so the snapshot cannot drift silently. Path coverage is `59/59` and a typed method exists for
  each pinned operation; this is a statement about the pinned document and **not** a claim that a live
  sandbox has answered all `67` operations.
- The two callback resend operations no longer share an operationId: the refreshed document names them
  `resendAlternativePaymentCallback` and `resendPayPartsCallback` instead of `resendCallback`. This is a
  documentation change only — both are unchanged on the wire, and the corresponding SDK methods were not
  renamed or rewired. No non-empty operationId in the pinned document is duplicated any more.
- `BaseService` gains an additive `PatchAsync` overload that takes a static log label, so a `PATCH` whose
  request target carries a caller identifier logs the route template instead. The existing overload keeps
  its previous logging behaviour.
- `BaseService` gains `PostWithoutBodyAsync`, for official `POST` operations that declare no request body.
  It leaves `HttpRequestMessage.Content` null rather than sending an invented `{}`, and reuses the
  existing retry, error-mapping, and deserialization behaviour.
- `BaseService.HandleErrorResponse` and `BaseService.ExecuteWithRetryAsync` changed from `private` to
  `protected` so an operation needing its own transport reuses the single status-to-exception switch and
  the single retry loop instead of duplicating either. Both additive; no status-to-exception mapping
  changed.
- `RozetkaPayClient.Dispose` also releases the decline client the payment-instruction service created
  internally. Disposal stays idempotent, and an externally supplied `HttpClient` is still not disposed.
- The safe-label `PatchAsync` overload and `PostWithoutBodyAsync` dispose their `HttpResponseMessage`
  deterministically, on the success path and on the path where the status-to-exception mapper throws. The
  pre-existing two-argument `PatchAsync` delegates to the new overload and inherits that fix; its signature,
  request target, body and logging are unchanged.
- **`AddRozetkaPay` now removes the built-in `IHttpClientFactory` HTTP logging from both of its named
  clients** (`RozetkaPay` and `RozetkaPay.PaymentInstructions.Decline`), so entries under
  `System.Net.Http.HttpClient.RozetkaPay.*` — `LogicalHandler` and `ClientHandler` alike — are no longer
  emitted for any SDK operation. This is a deliberate privacy fix, not a convenience change: that logging
  writes the request URI, and while `Microsoft.Extensions.Http` redacts the whole query to `?*` it does
  **not** redact path segments, so the `subscription_id` of the new payment-method update reached the log
  verbatim at Information level. Its header logging also redacts values in the rendered message only,
  leaving the real `Authorization` and `X-CUSTOMER-AUTH` values in the structured state at Trace level.
  Neither is configurable, so the loggers are removed with the supported `RemoveAllLoggers()` API.
- Removing those loggers does not change what the SDK's **own** service logging emits, and no SDK-wide
  guarantee about that logging is made here. The ten operations added in this entry log a static route
  template and, in the logging their tests capture under the default disabled retry policy, no caller
  identifier, credential, request body, response body, `RozetkaPayApiError.RawBody`, or decline `Location`.
  Every pre-existing operation logs exactly what it logged before and is **not** audited or claimed to be
  identifier- or content-safe: most log the real request target, several routes embed a caller identifier in
  it (`/api/customers/v1/{customerId}/cards/{cardId}`,
  `/api/alternative-payments/v1/operation/{externalId}`, `/api/payparts/v1/operation/{operationId}`, and the
  query strings of list operations), `PaymentService.ConfirmP2PAsync` logs the external ID and the amount,
  and the shared retry warning includes the transport exception message when retries are enabled. See the
  Logging section of the package README for the exact scope. Applications that need request-level HTTP
  telemetry should add their own `DelegatingHandler` or `IHttpClientLogger` that logs a redacted target.
- `DeletePaymentFromWalletAsync`, `GetCustomerSubscriptionsAsync` and `CancelAsync` are now
  `[Obsolete]` on both the interfaces and the implementations, each naming its canonical replacement.
  This is a compile-time warning only: their route, HTTP verb, request body and response type are
  unchanged on the wire, and every pre-existing signature stays binary compatible.
- `SubscriptionList` keeps its public class, `Subscriptions` property, property type and base type; an
  internal `JsonConverter<SubscriptionList>` now maps the official root JSON array onto it. Reads
  accept both the official array and the historical `{ "subscriptions": [...] }` wrapper; writes emit
  the official root array. An official `[]` yields an empty list, a wrapper carrying
  `"subscriptions": null` yields `null`, and an absent list is normalized to `[]` when serializing.
- Every DELETE request now rejects an already-cancelled `CancellationToken` before the retry loop and
  before `HttpClient` is invoked, so no DELETE reaches an `HttpMessageHandler` after the caller has
  cancelled. This previously depended on a `HttpClient` pre-dispatch check that behaves differently on
  `net9.0` and `net10.0`.
- The exception hierarchy and every pre-existing public exception constructor are unchanged:
  `RozetkaPayException`, `RozetkaPayAuthorizationException`, `RozetkaPayValidationException`,
  `RozetkaPayRateLimitException`, and `RozetkaPayNotFoundException` keep their parameterless,
  `(string)`, and `(string, Exception)` constructors, none is obsolete, and the status-to-exception
  mapping is untouched. `ApiError` is an added get-only property, and the structured construction path
  is internal, so `new RozetkaPayException("message", null)` stays unambiguous.
- The API error log line changed from `API error response received. StatusCode: … Message: …` to
  `RozetkaPay API error. StatusCode: … ApiCode: … RequestId: …`; the parsed provider message is no
  longer logged. Exception messages themselves are unchanged, and error-message parsing additionally
  accepts a nested `error.message`.
- A push to `main` no longer publishes a NuGet package; publishing now happens
  only when a version tag is pushed.
- `AddRozetkaPay` now builds its configuration snapshot from the validated options. The existing
  `RozetkaPayConfiguration`, `IConfiguration`, and login/password overloads, their
  registration-time failures, and every service lifetime are unchanged; the snapshot stays
  resolvable from DI. Settings that the previous check let through but the SDK cannot use — a
  `BaseUrl` that is well formed but not `http`/`https`, a non-positive `Timeout`, a
  whitespace-only login or password, an inconsistent retry policy — are now rejected while the
  host starts instead of failing on the first request.

### Removed
- **Breaking (pre-1.0):** `RozetkaPayOptions.ValidateSslCertificate` and
  `RozetkaPayConfiguration.ValidateSslCertificate` are removed. Both were dead: neither was ever read
  by a service, by the DI registration, or by any `HttpMessageHandler`, so setting either to `false`
  changed no TLS behaviour. The switch only ever promised something the SDK did not do, and removing
  the false promise is the point of this change — no certificate-validation feature was added, and no
  replacement setting exists. Code that assigned the property now fails to compile; delete the
  assignment.
- The removal was chosen over wiring the switch up. Connecting it would have meant putting a
  certificate-validation bypass into a payment SDK, and it could not have been applied consistently
  anyway: the SDK is constructed through a DI named `HttpClient`, an internally owned direct client, a
  caller-supplied `HttpClient`, and direct service construction, and a handler switch cannot reach an
  `HttpClient` a caller already built. Doing this before the stable 1.0 contract is the right
  compatibility boundary.
- TLS certificate validation is unchanged and always follows the platform or caller-supplied
  `HttpMessageHandler` policy. The SDK installs no certificate callback, never disables platform
  validation, and does not inspect or replace a caller's handler.
- `AddRozetkaPay(IConfiguration)` now throws `InvalidOperationException` when the removed
  `RozetkaPay:ValidateSslCertificate` key is still present, whatever its value, instead of letting the
  binder ignore it — a silently dropped key would leave an operator believing a TLS policy they
  configured is still in force. The message names the key, says why it was removed, and never contains
  its value or any neighbouring setting. **Migration: remove the key from your configuration.** Nothing
  else needs to change.
- To trust a certificate the platform does not: install the CA in the operating-system trust store
  (production), or build the `HttpClient` yourself with a handler narrowed to that one certificate and
  pass it to the SDK (local or test infrastructure). Production code must never install a trust-all
  callback such as `DangerousAcceptAnyServerCertificateValidator` or a
  `ServerCertificateCustomValidationCallback` that returns `true`: it disables authentication of the
  payment endpoint and exposes credentials and card data to interception.

### Fixed
- Operation-level parity defects against the pinned OpenAPI snapshot
  (`docs/openapi.json`, SHA-256 `309e61bf2185706c137f2d270d767b31777f7a4d09f2f2e0fb900fe36601cc44`,
  `49` paths / `57` operations):
  - `deleteCustomerPayment` was called as `DELETE /api/customers/v1/{customerId}/cards/{cardId}` with
    no request body and deserialized into `DeleteCardFromWalletResponse` instead of
    `DeleteCustomerPaymentResult`.
  - `getSubscriptions` was called as
    `GET /api/subscriptions/v1/subscriptions/customer/{customerId}` and deserialized into the
    `CustomerSubscriptionsResponse` wrapper instead of the official root array.
  - `CancelCustomerSubscription` was sent as a `POST` with an `external_id`/`reason`/`immediate` JSON
    body and returned no typed response, instead of a bodiless `DELETE` with optional `external_id`
    and `refund` query parameters returning `DefaultResponse`.
  No canonical call ever falls back to the legacy path or verb, on `404` or on any other failure: the
  request and response shapes genuinely differ, and `reason` and `immediate` have no honest mapping onto
  `refund`, so a fallback would conceal a parity error. A canonical `404` makes exactly one HTTP request.
  Retry behaviour is unchanged by this release: with a retry policy enabled, the SDK may repeat the
  **same** canonical request target for the conditions it already supports — transport-level failures and
  `429` — always as the same operation against the same target, and never as a different route or verb.
- `docs/API_COMPATIBILITY.md` no longer implies that path presence equals operation parity, and now
  reports the pinned snapshot and the live official document as two separate views.
- Every dynamic query value the SDK puts into a request URI is now percent-encoded as an
  individual value. A caller value can no longer add or overwrite a query parameter
  (`external_id=pay&status=success` stays one `external_id`), start a fragment (`#`), smuggle a
  raw `?`, or reach the path with `/`. This covers `PaymentService.GetInfoAsync`,
  `GetReceiptAsync`, and `GetListAsync`; `PayoutService.GetInfoAsync` and `GetListAsync`;
  `PayPartsService.GetOperationsAsync`; `AlternativePaymentService.GetOperationsAsync`; and the
  FinMon P2P pre-limits query. Endpoints that already encoded their values — the customer wallet
  endpoints, the payout account balance, and the PayParts/Alternative Payments info endpoints —
  are unchanged and are still encoded exactly once.
- Encoding semantics: a space becomes `%20` (never `+`), a literal `+` becomes `%2B`, and `&`,
  `=`, `?`, `#`, `/`, `%` become `%26`, `%3D`, `%3F`, `%23`, `%2F`, `%25`. Non-ASCII text is sent
  as UTF-8 percent-encoded octets. RFC 3986 unreserved characters stay readable, so request URIs
  built from ordinary IDs, dates, and pagination values are byte-for-byte what they were before.
- Callers pass raw values. The SDK encodes once, so a value that already looks encoded is treated
  as literal text: `already%2Fencoded` is sent as `already%252Fencoded`. Do not pre-encode.
- List filter dates and pagination are formatted with `CultureInfo.InvariantCulture`. Under a
  non-Gregorian ambient culture the `yyyy-MM-dd` filters previously rendered in that calendar —
  `th-TH` turned `2026-02-28` into `2569-02-28` — and a culture-specific negative sign could reach
  the query. Both now always render invariantly.
- A whitespace-only string filter is still sent, and now arrives as its encoded characters
  (`status=%20%20%20`) instead of being truncated to an empty value by URI canonicalization. Null
  and empty filters are still omitted, and query parameter names, ordering, and endpoint paths are
  unchanged.
- Every caller-controlled identifier the SDK puts into a request **path** is now percent-encoded as
  an individual path segment. Previously such an identifier was interpolated raw, so a `/` added a
  path segment, a `?` started a query, and a `#` started a fragment and silently discarded the rest
  of the identifier. Input that already looked like a percent escape was passed through as an escape
  rather than as data, so `already%2Fencoded` went on the wire unchanged and a server decodes it to
  `already/encoded`. Non-ASCII text, spaces, and a `%` that does not form a valid escape were already
  percent-encoded correctly by `System.Uri` and were never misrouted; they are covered by the new
  tests as regression protection, not as fixed defects. This covers
  `AlternativePaymentService.GetOperationInfoAsync` and
  `GetStatusAsync`; `PayPartsService.GetOperationInfoAsync`; `CustomerService.GetCustomerCardsAsync`
  and `DeletePaymentFromWalletAsync` (both segments encoded independently); and every
  `SubscriptionService` plan and subscription route — `GetPlanAsync`, `UpdatePlanAsync`,
  `DeactivatePlanAsync`, `GetAsync`, `UpdateAsync`, `DeactivateAsync`, `GetPaymentsAsync`,
  `CancelAsync`, and `GetCustomerSubscriptionsAsync`.
- This is the path-segment counterpart of the query-value encoding fixed earlier in this release.
  A path segment and a query value are separate contexts and are encoded at their own insertion
  points; the compatibility fallback paths that already encoded their segments are unchanged on the
  wire and are still encoded exactly once.
- Callers pass raw identifiers and must not pre-encode: `already%2Fencoded` is sent as
  `already%252Fencoded`. An identifier made only of RFC 3986 unreserved characters, such as
  `plan-123`, reaches the wire byte-for-byte unchanged.
- The identifiers `.` and `..` are now rejected with `ArgumentException` naming the offending
  parameter, before any request is sent. They cannot be carried through as data: `.` is an unreserved
  character that percent-encoding leaves alone, and `System.Uri` removes exact dot segments — also
  from the `%2E` spelling — while building the request, which would silently retarget the call
  (`GetPlanAsync(".")` requested `/api/subscriptions/v1/plans/`). A method with a path fallback
  rejects such an identifier before its primary query request.
- Endpoint paths, HTTP verbs, request bodies, primary/fallback order, the 404-only fallback trigger,
  public interfaces, and models are unchanged.

## [0.1.0-alpha.2] - 2026-02-28

### Fixed
- NuGet README maintainer image rendering.

## [0.1.0-alpha.1] - 2026-02-28

### Added
- Initial alpha SDK package.

[Unreleased]: https://github.com/i7aket/SYT.RozetkaPay/commits/main
[0.1.0-alpha.2]: https://www.nuget.org/packages/SYT.RozetkaPay/0.1.0-alpha.2
[0.1.0-alpha.1]: https://www.nuget.org/packages/SYT.RozetkaPay/0.1.0-alpha.1
