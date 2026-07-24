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

## [0.1.0-alpha.2] - 2026-02-28

### Fixed
- NuGet README maintainer image rendering.

## [0.1.0-alpha.1] - 2026-02-28

### Added
- Initial alpha SDK package.

[Unreleased]: https://github.com/i7aket/SYT.RozetkaPay/commits/main
[0.1.0-alpha.2]: https://www.nuget.org/packages/SYT.RozetkaPay/0.1.0-alpha.2
[0.1.0-alpha.1]: https://www.nuget.org/packages/SYT.RozetkaPay/0.1.0-alpha.1
