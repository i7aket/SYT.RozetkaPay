# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Maintainers move entries out of `Unreleased` into a new versioned section
immediately before tagging a release (see the release process in `README.md`).

## [Unreleased]

### Added
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
- A push to `main` no longer publishes a NuGet package; publishing now happens
  only when a version tag is pushed.
- `AddRozetkaPay` now builds its configuration snapshot from the validated options. The existing
  `RozetkaPayConfiguration`, `IConfiguration`, and login/password overloads, their
  registration-time failures, and every service lifetime are unchanged; the snapshot stays
  resolvable from DI. Settings that the previous check let through but the SDK cannot use — a
  `BaseUrl` that is well formed but not `http`/`https`, a non-positive `Timeout`, a
  whitespace-only login or password, an inconsistent retry policy — are now rejected while the
  host starts instead of failing on the first request.

## [0.1.0-alpha.2] - 2026-02-28

### Fixed
- NuGet README maintainer image rendering.

## [0.1.0-alpha.1] - 2026-02-28

### Added
- Initial alpha SDK package.

[Unreleased]: https://github.com/i7aket/SYT.RozetkaPay/commits/main
[0.1.0-alpha.2]: https://www.nuget.org/packages/SYT.RozetkaPay/0.1.0-alpha.2
[0.1.0-alpha.1]: https://www.nuget.org/packages/SYT.RozetkaPay/0.1.0-alpha.1
