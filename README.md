# SYT.RozetkaPay

`SYT.RozetkaPay` is a .NET SDK for RozetkaPay API integration.

## Package

- Package ID: `SYT.RozetkaPay`
- Target frameworks: `net9.0`, `net10.0`
- License: `MIT`
- Versioning: derived from Git release tags via [MinVer](https://github.com/adamralph/minver)

Consumer installation and usage documentation lives in the package README
(`src/SYT.RozetkaPay/README.md`), which is also shipped on NuGet.

## Public API

The SDK exposes each service through a public interface and the whole surface through a
single aggregate contract, so consumers can depend on abstractions and substitute them in
unit tests:

- `IRozetkaPayClient` — aggregate contract, derives from `IDisposable`
- `IPaymentService`, `IBatchPaymentService`, `IPayPartsService`, `IPayoutService`,
  `ICustomerService`, `ISubscriptionService`, `IReportService`,
  `IAlternativePaymentService`, `IMerchantService`, `IFinMonService`
- `IRozetkaPayWebhookSignatureVerifier` — verifies the `X-ROZETKAPAY-SIGNATURE` header on
  incoming callbacks
- `RozetkaPayOptions` / `RozetkaPayEnvironment` — typed settings bound from the `RozetkaPay`
  configuration section, resolvable as `IOptions<RozetkaPayOptions>`

`AddRozetkaPay` registers both the interface and the concrete type, and each pair resolves
to the same DI-managed instance. API services are scoped; the immutable webhook verifier is
a singleton. The concrete types stay public and unchanged, so existing code continues to
compile. Details and testing examples are in the package README.

Configuration goes through the options pattern: set `Environment` to `Production` (the default) or
`Sandbox` to pick the endpoint instead of writing a URL, and the settings are validated at startup
so a broken configuration fails before the first request instead of during one. The pre-existing
`RozetkaPayConfiguration` overloads are unchanged. See
[Configuration](src/SYT.RozetkaPay/README.md#configuration) in the package README.

Callback signatures must be checked against the raw request body before the payload is
deserialized; the package README documents the full flow under
[Webhook Signature Verification](src/SYT.RozetkaPay/README.md#webhook-signature-verification).

Failed API calls carry structured details: `RozetkaPayException.ApiError` exposes a
`RozetkaPayApiError` with the HTTP status, the provider error code as text, the request ID, and the
raw response body. Treat the raw body as sensitive — the SDK never logs it. See
[Error Handling](src/SYT.RozetkaPay/README.md#error-handling) in the package README.

## Repository Structure

- SDK source: `src/SYT.RozetkaPay`
- Tests: `tests/SYT.RozetkaPay.Tests`
- Continuous integration: `.github/workflows/ci.yml`
- Release / publish: `.github/workflows/release.yml`
- Changelog: `CHANGELOG.md`

## Continuous Integration

Every pull request targeting `main`, and every push to `main`, runs the
`Build & Test` workflow, which:

1. Restores and builds the solution in `Release` with warnings treated as errors.
2. Runs the full test suite on `net9.0` and `net10.0`.
3. Packs the NuGet package and verifies the produced `.nupkg`/`.snupkg`.

This workflow is read-only: it uses no repository secrets and never publishes.
A merge to `main` builds and tests the default branch but **does not** publish a
package.

## Versioning

The package version is not stored in the project file. It is computed by MinVer
from the Git history:

- On an untagged commit, MinVer produces a unique prerelease version that is
  never published.
- On a commit tagged `vX.Y.Z[-prerelease]`, the package version is exactly
  `X.Y.Z[-prerelease]`.

The release tag prefix is `v` and the minimum major/minor is `0.1`.

## Releasing (maintainers)

Publishing to NuGet and creating a GitHub Release happen only when a version tag
matching `v*.*.*` is pushed. To cut a release:

1. Update `CHANGELOG.md`: move the relevant `Unreleased` entries into a new
   `## [X.Y.Z]` section dated for the release.
2. Make sure `main` is green on the `Build & Test` workflow.
3. Create and push a SemVer tag (and only the tag):

```bash
git switch main
git pull --ff-only origin main
git tag -a v0.1.0-alpha.3 -m "SYT.RozetkaPay v0.1.0-alpha.3"
git push origin v0.1.0-alpha.3
```

Pushing the tag triggers the `Release NuGet` workflow, which:

1. Validates that the tag is a well-formed `vX.Y.Z[-prerelease]` version and
   fails before publishing on a malformed tag.
2. Restores, builds (`-warnaserror`), tests both target frameworks, and packs.
3. Confirms the packed version matches the tag.
4. Publishes the package (and symbols) to nuget.org.
5. Creates a GitHub Release with the `.nupkg`, `.snupkg`, and `SHA256SUMS`
   attached. Tags containing a prerelease label are marked as pre-releases.

### Required secret

The workflow reads the NuGet API key from the `NUGET_API_KEY` repository secret,
which must be configured in repository settings. The key value is never printed
and must never be committed to the repository. If the secret is missing, the
release fails before any publish step runs.

### Re-running a release

`dotnet nuget push` uses `--skip-duplicate`, so re-running a release for a tag
already published to NuGet succeeds without creating a duplicate. The GitHub
Release step intentionally fails if a release already exists for the tag, so an
existing release is never silently overwritten; adjust the existing release
manually if a rerun must recreate it.

## Maintainer

Maintained by **Anatoliy Yermakov** for RozetkaPay integrators.
