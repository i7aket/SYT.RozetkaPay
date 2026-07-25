# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Maintainers move entries out of `Unreleased` into a new versioned section
immediately before tagging a release (see the release process in `README.md`).

## [Unreleased]

### Added
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
