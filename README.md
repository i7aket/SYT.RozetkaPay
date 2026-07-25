# SYT.RozetkaPay

`SYT.RozetkaPay` is a .NET SDK for RozetkaPay API integration.

## Package

- Package ID: `SYT.RozetkaPay`
- Target frameworks: `net9.0`, `net10.0`
- License: `MIT`
- Versioning: derived from Git release tags via [MinVer](https://github.com/adamralph/minver)

Consumer installation and usage documentation lives in the package README
(`src/SYT.RozetkaPay/README.md`), which is also shipped on NuGet.

The package ships an embedded `128x128` icon and a companion `.snupkg` symbol package with
Source Link metadata, so a debugger can step from the compiled assembly straight into the
exact repository source the package was built from:

- Icon: `assets/package-icon.png`, packed into the package root as `package-icon.png`. It is
  an original SDK mark generated from the committed `assets/package-icon.svg` — not the
  RozetkaPay logo and not derived from any third-party asset.
- Symbols: `lib/net9.0/SYT.RozetkaPay.pdb` and `lib/net10.0/SYT.RozetkaPay.pdb`, published in
  the `.snupkg` only. The primary `.nupkg` carries no PDB.
- Source Link comes from the tooling built into the .NET SDK. There is deliberately **no**
  `Microsoft.SourceLink.*` `PackageReference`, so the published dependency groups stay exactly
  `net9.0` and `net10.0` with no build-only package leaking into them.
- Official CI and release builds run with `ContinuousIntegrationBuild=true`, so every source
  path embedded in the symbols is normalized to `/_/*`. No runner, machine or worktree
  filesystem root is ever published.

Each PDB carries a single Source Link mapping pinned to the commit that produced it:

```json
{
  "documents": {
    "/_/*": "https://raw.githubusercontent.com/i7aket/SYT.RozetkaPay/<commit>/*"
  }
}
```

## Public API

The SDK exposes each service through a public interface and the whole surface through a
single aggregate contract, so consumers can depend on abstractions and substitute them in
unit tests:

- `IRozetkaPayClient` — aggregate contract, derives from `IDisposable`
- `IPaymentService`, `IBatchPaymentService`, `IPayPartsService`, `IPayoutService`,
  `ICustomerService`, `ISubscriptionService`, `IReportService`,
  `IAlternativePaymentService`, `IMerchantService`, `IFinMonService`,
  `IInStorePaymentService`, `IPartnerService`, `IPaymentInstructionService`
- `IRozetkaPayWebhookSignatureVerifier` — verifies the `X-ROZETKAPAY-SIGNATURE` header on
  incoming callbacks
- `RozetkaPayOptions` / `RozetkaPayEnvironment` — typed settings bound from the `RozetkaPay`
  configuration section, resolvable as `IOptions<RozetkaPayOptions>`

`AddRozetkaPay` registers both the interface and the concrete type, and each pair resolves
to the same DI-managed instance. API services are scoped; the immutable webhook verifier is
a singleton. The concrete types stay public and unchanged, so existing code continues to
compile. Details and testing examples are in the package README.

The SDK is pinned to the official OpenAPI document observed on `2026-07-25` — `59` paths and
`67` operations — and exposes a typed method for each of those operations.

Every one of those `67` operations is covered by an executable contract test: the SDK method is
invoked and the request it produces is asserted against the pinned document, which is compared
with the test manifest as an exact set. Outbound authentication, the anonymous decline redirect,
and the inbound webhook signature pipeline are additionally proven against a real Kestrel server
on loopback. All of that runs in ordinary CI on both target frameworks and needs no network.

That is a statement about the pinned document and about what the SDK puts on the wire — **not** a
claim that a live RozetkaPay environment answered all 67. Most published operations move real
money and are never called live. The single live check is an opt-in, read-only merchant identity
call that is skipped unless `ROZETKAPAY_SANDBOX_LOGIN` and `ROZETKAPAY_SANDBOX_PASSWORD` are both
set; no sandbox credentials are configured in CI, so it does not run there. See
[API compatibility](src/SYT.RozetkaPay/docs/API_COMPATIBILITY.md).

`declinePaymentInstruction` is the one unauthenticated operation. The SDK sends it over a
dedicated credential-free client whose handler does not follow redirects, returns the `Location`
header, and never fetches the target — see
[Payment Instructions](src/SYT.RozetkaPay/README.md#payment-instructions).

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

1. Restores the pinned local tools (`.config/dotnet-tools.json`) and the solution.
2. Builds in `Release` with warnings treated as errors and
   `-p:ContinuousIntegrationBuild=true`, so the produced symbols contain no
   machine-specific source root.
3. Runs the full test suite on `net9.0` and `net10.0`.
4. Rebuilds the same commit in a second, throwaway `git worktree` under a
   different filesystem root and requires the `SYT.RozetkaPay.dll`, `.pdb` and
   `.xml` of both frameworks to be identical by SHA-256
   (`scripts/verify-deterministic-build.sh`).
5. Packs the NuGet package and verifies the produced `.nupkg`/`.snupkg`
   (`scripts/verify-package-artifacts.sh`).

The artifact verifier inspects archive contents rather than file names. It proves the
packed icon really is a `128x128` PNG under `1 MiB` and byte-identical to the committed
asset, that the nuspec keeps its `id`, `icon`, `readme` and `license` metadata and its
`net9.0`/`net10.0` dependency groups, that the `<repository>` element records the exact
commit that was checked out, that the primary package carries no PDB while the `.snupkg`
carries exactly the two, and that each PDB has a single Source Link mapping to that commit
with every source document normalized under `/_/`. It then runs `dotnet sourcelink test`,
which downloads every source document for that commit and compares checksums.

Both verifiers are ordinary scripts, so the same gates can be reproduced locally:

```bash
dotnet tool restore
dotnet restore SYT.RozetkaPay.sln
dotnet build SYT.RozetkaPay.sln -c Release --no-restore -warnaserror \
  -p:ContinuousIntegrationBuild=true
dotnet test SYT.RozetkaPay.sln -c Release --no-build

scripts/verify-deterministic-build.sh

artifact_dir="$(mktemp -d)"
dotnet pack src/SYT.RozetkaPay/SYT.RozetkaPay.csproj -c Release --no-build \
  -p:ContinuousIntegrationBuild=true -o "$artifact_dir"
scripts/verify-package-artifacts.sh "$artifact_dir" "$(git rev-parse HEAD)"
```

`scripts/verify-deterministic-build.sh` requires tracked files to match `HEAD`, creates its
second checkout under `mktemp -d`, and removes only that temporary worktree. Add
`--skip-remote-source-check` to `scripts/verify-package-artifacts.sh` when the commit has not
been pushed yet — its sources cannot be on the remote, so only then does the download check
have to be skipped. CI and release builds never skip it.

The suite includes the `67`-operation contract coverage and the loopback HTTP-boundary
tests, and it makes no outbound network request: the contract transport targets a reserved
`.invalid` host and the boundary tests bind `127.0.0.1` on an ephemeral port. CI therefore
does not depend on RozetkaPay being reachable.

The live sandbox smoke test is reported as **skipped** in CI, because no sandbox credentials
are configured as repository secrets. That is deliberate: a workflow that went green merely
because a secret was absent would be claiming live verification it never performed. Run it
manually instead — see
[API compatibility](src/SYT.RozetkaPay/docs/API_COMPATIBILITY.md).

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
2. Confirms the tagged commit is reachable from `origin/main`.
3. Restores tools and the solution, builds (`-warnaserror`,
   `-p:ContinuousIntegrationBuild=true`), tests both target frameworks, proves the
   deterministic two-root rebuild, packs, and runs the **same**
   `scripts/verify-package-artifacts.sh` gate the pull-request workflow runs —
   icon, nuspec, repository commit and remote Source Link checksums — all before
   any publish step.
4. Confirms the packed version matches the tag.
5. Publishes the package (and symbols) to nuget.org.
6. Creates a GitHub Release with the `.nupkg`, `.snupkg`, and `SHA256SUMS`
   attached. Tags containing a prerelease label are marked as pre-releases.

A release therefore never passes weaker artifact checks than a pull request: the shared
verifier runs first, and the exact tag/version gate still stands between it and publish.

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
