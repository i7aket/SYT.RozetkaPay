# SDK Contract Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring `SYT.RozetkaPay` in line with the published RozetkaPay API — close two security blockers, two correctness blockers, and the CI gap that let all of it ship in 1.0.0.

**Architecture:** The SDK is a thin typed layer over `HttpClient`: `BaseService` owns transport, retry and serialization; thirteen services derive from it; DI wiring lives in `ServiceCollectionExtensions`. Every fix here stays inside those seams — no restructuring. The one structural addition is a CI job that treats the live OpenAPI document, not a committed snapshot, as the source of truth.

**Tech Stack:** .NET 9 / .NET 10 (multi-target), xUnit, `System.Text.Json`, `Microsoft.Extensions.*` 9.0.x, MinVer, GitHub Actions.

## Global Constraints

- Target frameworks are `net9.0;net10.0`. Every test must pass on both — `dotnet test` runs both by default.
- `TreatWarningsAsErrors` is on repo-wide (`Directory.Build.props`). A warning fails the build.
- `AnalysisLevel` is `latest`. Do not add `NoWarn`, `WarningsNotAsErrors`, or suppressions to silence a new diagnostic — fix the code.
- Nullable reference types are enabled. No `!` to paper over a real nullability question.
- `GenerateDocumentationFile` is on and the codebase currently has **zero** undocumented public members. Every new public member needs an XML doc comment.
- **Breaking changes are allowed and preferred where the alternative leaves an incorrect contract.** The package has no external consumers. Do not add `[Obsolete]` shims to preserve compatibility — delete the wrong thing.
- One work item = one ticket = one branch = one PR. Branch name `<type>/EXP-NNN-<slug>`, commit and PR title prefixed `EXP-NNN`.
- Every PR gets an independent Codex review before merge (see "Codex review protocol" at the end).
- Never commit `AUDIT-CODEX.md`, `sdk-model-dump.json`, or anything from the session scratchpad. `scripts/verify-repository-hygiene.sh` enforces the tracked-file set — run it before committing if you added files.
- The live OpenAPI document is `https://docs.rozetkapay.com/openapi.json`. The committed snapshot is `src/SYT.RozetkaPay/docs/openapi.json`. When they disagree, **the live document wins**.

---

## File Structure

New files:

| File | Responsibility |
|---|---|
| `tests/SYT.RozetkaPay.Tests/RedirectSecurityTests.cs` | Proves no secret header survives a 3xx, and that a redirect is not followed. |
| `tests/SYT.RozetkaPay.Tests/EnumWireTokenTests.cs` | Asserts every SDK enum serializes to the exact token set the spec publishes. |
| `tests/SYT.RozetkaPay.Tests/TestInfrastructure/SpecEnumSource.cs` | Reads enum value sets out of the snapshot so the enum test is data-driven, not hand-copied. |
| `tests/SYT.RozetkaPay.Tests/ConsumerHttpClientOwnershipTests.cs` | Proves the SDK does not write to a client it does not own. |
| `tests/SYT.RozetkaPay.Tests/DateTimeConversionTests.cs` | Covers Utc / Local / Unspecified / Unix across read and write. |
| `tests/SYT.RozetkaPay.Tests/PartialCaptureContractTests.cs` | Covers partial confirm and cancel bodies. |
| `scripts/verify-openapi-drift.sh` | Downloads the live document, diffs it semantically against the snapshot, exits non-zero on drift. |

Modified files carry their line ranges in each task.

---

## Task 1: Forbid redirects on authenticated transport and non-TLS endpoints

**Ticket:** [EXP-383](https://experthub.youtrack.cloud/issue/EXP-383) (W1) · Branch `fix/EXP-383-redirect-and-tls`

**Files:**
- Modify: `src/SYT.RozetkaPay/Extensions/ServiceCollectionExtensions.cs:266-278`
- Modify: `src/SYT.RozetkaPay/Configuration/RozetkaPayOptions.cs`
- Modify: `src/SYT.RozetkaPay/Configuration/RozetkaPayOptionsValidator.cs:152-160`
- Modify: `tests/SYT.RozetkaPay.Tests/RozetkaPayOptionsTests.cs:590-601`
- Test: `tests/SYT.RozetkaPay.Tests/RedirectSecurityTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks — this is the first task.
- Produces: `RozetkaPayOptions.AllowInsecureLoopbackTransport` (`bool`, default `false`). Later tasks that build options in tests for a loopback HTTP endpoint must set it.

**Why this is first:** it is the only finding where the defect leaks a credential. Everything else is correctness.

- [ ] **Step 1: Write the failing redirect test**

Create `tests/SYT.RozetkaPay.Tests/RedirectSecurityTests.cs`:

```csharp
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Extensions;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// A 302 from the payment host must never become a second request carrying the merchant's
/// credentials. .NET strips Authorization across origins but forwards custom headers, so the
/// SDK cannot rely on the runtime for this guarantee and must refuse to follow redirects.
/// </summary>
public class RedirectSecurityTests
{
    [Fact]
    public void AuthenticatedClient_ShouldNotFollowRedirects()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = "test-login";
            options.Password = "test-password";
            options.OnBehalfOf = "on-behalf-secret";
            options.CustomerAuth = "customer-auth-secret";
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

        // The named client the authenticated services resolve. Its primary handler is what
        // decides whether a 302 is followed, and it is the thing under test.
        HttpMessageHandler handler = ResolvePrimaryHandler(factory, "RozetkaPay");

        HttpClientHandler clientHandler = Assert.IsType<HttpClientHandler>(handler);
        Assert.False(clientHandler.AllowAutoRedirect);
    }

    private static HttpMessageHandler ResolvePrimaryHandler(IHttpClientFactory factory, string name)
    {
        // IHttpClientFactory does not expose the handler chain, so the test walks it the same way
        // the runtime builds it: create the client, then read the private handler field.
        HttpClient client = factory.CreateClient(name);
        object? current = typeof(HttpMessageInvoker)
            .GetField("_handler", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(client);

        while (current is DelegatingHandler delegating)
        {
            current = delegating.InnerHandler;
        }

        return (HttpMessageHandler)current!;
    }
}
```

- [ ] **Step 2: Run it and confirm it fails**

Run: `cd /Volumes/T9/Projects/RozetkaPay && dotnet test --filter "FullyQualifiedName~RedirectSecurityTests" -f net10.0`
Expected: FAIL — `AllowAutoRedirect` is `true`, because the authenticated client never configures a primary handler.

- [ ] **Step 3: Configure the authenticated client's primary handler**

In `src/SYT.RozetkaPay/Extensions/ServiceCollectionExtensions.cs`, on the `AuthenticatedHttpClientName` registration (currently ending `.RemoveAllLoggers();` at line 278), insert the handler configuration before `.RemoveAllLoggers()`:

```csharp
            // A 302 is never followed on an authenticated transport. HttpClientHandler drops
            // Authorization when the redirect crosses an origin, but forwards X-ON-BEHALF-OF and
            // X-CUSTOMER-AUTH verbatim - both are merchant secrets, and a compromised or merely
            // misconfigured host could harvest them by answering with a Location it controls.
            // Refusing the redirect outright is the only guarantee that does not depend on which
            // headers a given runtime decides to strip.
            .ConfigurePrimaryHttpMessageHandler(static () => new HttpClientHandler { AllowAutoRedirect = false })
```

- [ ] **Step 4: Run the test and confirm it passes**

Run: `dotnet test --filter "FullyQualifiedName~RedirectSecurityTests" -f net10.0`
Expected: PASS

- [ ] **Step 5: Write the failing TLS tests**

Append to `tests/SYT.RozetkaPay.Tests/RedirectSecurityTests.cs`:

```csharp
    [Theory]
    [InlineData("http://gateway.example.com")]
    [InlineData("http://203.0.113.10:8080/rozetkapay/")]
    public void Validation_ShouldRejectPlainHttpOnANonLoopbackHost(string baseUrl)
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = "test-login";
            options.Password = "test-password";
            options.BaseUrl = baseUrl;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException failure = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains("https", string.Join(" ", failure.Failures), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validation_ShouldRejectLoopbackHttpUnlessExplicitlyAllowed()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = "test-login";
            options.Password = "test-password";
            options.BaseUrl = "http://localhost:5005";
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());
    }

    [Fact]
    public void Validation_ShouldAcceptLoopbackHttpWhenExplicitlyAllowed()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = "test-login";
            options.Password = "test-password";
            options.BaseUrl = "http://localhost:5005";
            options.AllowInsecureLoopbackTransport = true;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
        Assert.NotNull(provider.GetRequiredService<RozetkaPayConfiguration>());
    }

    [Fact]
    public void Validation_ShouldRejectNonLoopbackHttpEvenWhenLoopbackIsAllowed()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = "test-login";
            options.Password = "test-password";
            options.BaseUrl = "http://gateway.example.com";
            options.AllowInsecureLoopbackTransport = true;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());
    }
```

Add to the file's using block: `using Microsoft.Extensions.Options;`.

- [ ] **Step 6: Run and confirm they fail**

Run: `dotnet test --filter "FullyQualifiedName~RedirectSecurityTests" -f net10.0`
Expected: the four TLS tests FAIL — `AllowInsecureLoopbackTransport` does not compile yet.

- [ ] **Step 7: Add the opt-in option**

In `src/SYT.RozetkaPay/Configuration/RozetkaPayOptions.cs`, add:

```csharp
    /// <summary>
    /// Allow a plain <c>http://</c> endpoint when, and only when, its host is a loopback address.
    /// </summary>
    /// <remarks>
    /// Off by default: the SDK carries a Basic credential and two secret headers on every request,
    /// and none of them may travel in clear text. The switch exists for integration tests that run
    /// a stub gateway on localhost, where there is no certificate to present and no network to
    /// observe. It never relaxes anything for a non-loopback host - see
    /// <see cref="RozetkaPayOptionsValidator"/>, which checks the host, not just the scheme.
    /// </remarks>
    public bool AllowInsecureLoopbackTransport { get; set; }
```

Then mirror it in `RozetkaPayOptionsMapper` (both `FromConfiguration`/`CopyInto`/`ToConfiguration` directions) and in `RozetkaPayConfiguration`, following exactly how an existing `bool`-shaped setting is carried across. Grep for an existing simple property to copy the pattern: `rg -n "UserAgent" src/SYT.RozetkaPay/Configuration/`.

- [ ] **Step 8: Enforce it in the validator**

In `src/SYT.RozetkaPay/Configuration/RozetkaPayOptionsValidator.cs`, replace `IsAbsoluteHttpUrl` (lines 152-160) and its two call sites so the scheme check knows about the opt-in:

```csharp
    /// <summary>
    /// Reject relative URLs, URLs the SDK cannot speak, and any clear-text endpoint that is not an
    /// explicitly permitted loopback address.
    /// </summary>
    /// <remarks>
    /// The host is checked, not only the scheme. Allowing <c>http</c> on the strength of a switch
    /// alone would let a single test-oriented setting downgrade a production gateway to clear text,
    /// which is precisely the failure this guard exists to prevent.
    /// </remarks>
    private static bool IsAcceptableEndpoint(string value, bool allowInsecureLoopback)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        return uri.Scheme == Uri.UriSchemeHttp && allowInsecureLoopback && uri.IsLoopback;
    }
```

Update both call sites to pass `options.AllowInsecureLoopbackTransport`, and change the two failure messages from "must be an absolute http or https URL" to name the real rule, for example:

```csharp
            failures.Add(
                $"{Key(nameof(RozetkaPayOptions.BaseUrl))} must be an absolute https URL. Plain http is " +
                $"accepted only for a loopback host and only when " +
                $"{Key(nameof(RozetkaPayOptions.AllowInsecureLoopbackTransport))} is true.");
```

- [ ] **Step 9: Flip the test that froze the old behaviour**

In `tests/SYT.RozetkaPay.Tests/RozetkaPayOptionsTests.cs:590-601`, the theory `Validation_ShouldAcceptAnAbsoluteHttpOrHttpsBaseUrl` currently asserts that `http://localhost:5005` and `http://127.0.0.1:5005/rozetkapay/` are valid. Reduce it to the https case and rename it:

```csharp
    [Theory]
    [InlineData("https://gateway.example.com")]
    [InlineData("https://gateway.example.com:8443/rozetkapay/")]
    public void Validation_ShouldAcceptAnAbsoluteHttpsBaseUrl(string baseUrl)
    {
        AssertValid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.BaseUrl = baseUrl;
        });
    }
```

The loopback cases it used to cover now live in `RedirectSecurityTests` with the opt-in set.

- [ ] **Step 10: Find every other test that relied on loopback http**

Run: `rg -n "http://(localhost|127\.0\.0\.1)" tests/`
For each hit, either set `AllowInsecureLoopbackTransport = true` on the options, or — if the test constructs `RozetkaPayConfiguration` directly and never goes through validation — leave it alone. Do not weaken the validator to accommodate a test.

- [ ] **Step 11: Run the full suite on both frameworks**

Run: `dotnet test -c Release`
Expected: 0 failed. If a test outside the two files above fails, it is telling you about a real behaviour change — read it before changing it.

- [ ] **Step 12: Commit**

```bash
cd /Volumes/T9/Projects/RozetkaPay
git checkout -b fix/EXP-383-redirect-and-tls
git add src/SYT.RozetkaPay/Extensions/ServiceCollectionExtensions.cs \
        src/SYT.RozetkaPay/Configuration/ \
        tests/SYT.RozetkaPay.Tests/RedirectSecurityTests.cs \
        tests/SYT.RozetkaPay.Tests/RozetkaPayOptionsTests.cs
git commit -m "EXP-383 fix(security): refuse redirects and clear-text endpoints"
```

---

## Task 2: Cache the serializer options

**Ticket:** [EXP-389](https://experthub.youtrack.cloud/issue/EXP-389) (W7) · Branch `perf/EXP-389-cache-serializer-options`

**Files:**
- Create: `src/SYT.RozetkaPay/Serialization/SdkSerializerOptions.cs`
- Modify: `src/SYT.RozetkaPay/Services/BaseService.cs:1226-1247`

**Interfaces:**
- Consumes: nothing from Task 1.
- Produces: `SYT.RozetkaPay.Serialization.SdkSerializerOptions.Value` (`static JsonSerializerOptions`) — the single place the converter list lives from here on. Tasks 3, 5 and 8 serialize through it in their tests, and Task 3 edits the enum converter inside it.

**Why this comes second:** it is a pure extraction with no behaviour change, and three later tasks need a serializer instance they can reach from a test. Doing it first means none of them has to build a throwaway copy that later has to be found and deleted.

- [ ] **Step 1: Write the failing test**

Add to `tests/SYT.RozetkaPay.Tests/CoreBehaviorCoverageTests.cs`, with `using SYT.RozetkaPay.Serialization;` at the top of the file:

```csharp
    [Fact]
    public void SerializerOptions_ShouldBeASingleSharedInstance()
    {
        // A fresh JsonSerializerOptions means a fresh contract cache: measured at 2.340 ms per
        // serialization against 0.002 ms shared, and the transport built one per call, twice per
        // request.
        Assert.Same(SdkSerializerOptions.Value, SdkSerializerOptions.Value);
    }
```

- [ ] **Step 2: Run and confirm it does not compile**

Run: `dotnet test --filter "SerializerOptions_ShouldBeASingleSharedInstance" -f net10.0`
Expected: build error — `SdkSerializerOptions` does not exist.

- [ ] **Step 3: Extract the options**

Create `src/SYT.RozetkaPay/Serialization/SdkSerializerOptions.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Converters;

namespace SYT.RozetkaPay.Serialization;

/// <summary>
/// The single <see cref="JsonSerializerOptions"/> instance every SDK request and response uses.
/// </summary>
/// <remarks>
/// One instance, deliberately. <see cref="JsonSerializerOptions"/> carries the reflection-derived
/// contract cache for every type it has seen, so a new instance per call throws that cache away and
/// rebuilds it: measured at roughly a thousandfold cost on this SDK's own models, paid twice per
/// request. The instance is frozen by <see cref="System.Text.Json"/> on first use, which is also
/// what makes sharing it across threads safe.
/// </remarks>
public static class SdkSerializerOptions
{
    /// <summary>The shared serializer configuration.</summary>
    public static JsonSerializerOptions Value { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                // Carried over verbatim, naming policy included. This task changes nothing about
                // what goes on the wire - Task 3 is where the enum tokens get fixed, and it edits
                // this line. Keeping the extraction behaviour-neutral is what lets the existing
                // suite prove the extraction itself was correct.
                new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower),
                new FlexibleDecimalConverter(),
                new FlexibleDecimalConverterNonNullable(),
                new FlexibleInt32Converter(),
                new FlexibleNullableInt32Converter(),
                new FlexibleInt64Converter(),
                new FlexibleNullableInt64Converter(),
            },
        };
    }
}
```

- [ ] **Step 4: Point `BaseService` at it**

Replace the body of `GetJsonSerializerOptions` with `return SdkSerializerOptions.Value;`, keeping the method so derived services still compile, and note in its XML docs that the instance is shared and must not be mutated.

- [ ] **Step 5: Run and confirm it passes**

Run: `dotnet test --filter "SerializerOptions_ShouldBeASingleSharedInstance" -f net10.0`
Expected: PASS

- [ ] **Step 6: Confirm the extraction changed no behaviour**

Run: `dotnet test -c Release`
Expected: 0 failed, with no test edited other than the one added in Step 1. If a serialization test moves, the extraction was not faithful — compare the two option sets field by field before going further.

- [ ] **Step 7: Commit**

```bash
git checkout -b perf/EXP-389-cache-serializer-options
git add src/SYT.RozetkaPay/ tests/SYT.RozetkaPay.Tests/
git commit -m "EXP-389 perf(json): share one serializer options instance"
```

---

## Task 3: Serialize enums to the tokens the spec publishes

**Ticket:** [EXP-384](https://experthub.youtrack.cloud/issue/EXP-384) (W2) · Branch `fix/EXP-384-enum-wire-tokens`

**Files:**
- Modify: `src/SYT.RozetkaPay/Serialization/SdkSerializerOptions.cs` (the enum converter, created in Task 2)
- Modify: `src/SYT.RozetkaPay/Models/Common/CommonModels.cs` (`CustomerCheckoutLocale`, `OperationType`, `ResponseCode`)
- Modify: `src/SYT.RozetkaPay/Models/AlternativePayments/AlternativePaymentModels.cs` (`AlternativePaymentProvider`)
- Modify: `src/SYT.RozetkaPay/Models/PayParts/PayPartsModels.cs` (`PayPartsPaymentMode`)
- Modify: `src/SYT.RozetkaPay/Models/Batch/BatchModels.cs` (`BatchPaymentMode`)
- Modify: `src/SYT.RozetkaPay/Models/Subscriptions/SubscriptionModels.cs` (`SubscriptionCallbackType`)
- Create: `tests/SYT.RozetkaPay.Tests/TestInfrastructure/SpecEnumSource.cs`
- Test: `tests/SYT.RozetkaPay.Tests/EnumWireTokenTests.cs`

**Interfaces:**
- Consumes: `SdkSerializerOptions.Value` from Task 2 — both as the thing the tests serialize through and as the file whose enum converter this task edits.
- Produces: enum member names change. Any later task naming an enum member must use the post-fix name. The renames that matter: `PayPartsPaymentMode.Single`/`.Installment` are **deleted** and replaced by `.Hosted`/`.Direct`.

- [ ] **Step 1: Write the spec-reading helper**

Create `tests/SYT.RozetkaPay.Tests/TestInfrastructure/SpecEnumSource.cs`:

```csharp
using System.Text.Json;

namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// Reads enum value sets straight out of the pinned OpenAPI snapshot.
/// </summary>
/// <remarks>
/// Hand-copying the token lists into the test would defeat the point: a typo in the copy would
/// agree with a typo in production. The snapshot is the same artefact the drift job checks against
/// the live document, so a stale snapshot fails there rather than silently weakening this test.
/// </remarks>
internal static class SpecEnumSource
{
    private static readonly Lazy<JsonDocument> Document = new(static () =>
    {
        string path = Path.Combine(
            Path.GetDirectoryName(typeof(SpecEnumSource).Assembly.Location)!,
            "openapi.json");

        return JsonDocument.Parse(File.ReadAllText(path));
    });

    /// <summary>
    /// The declared values of a named enum schema, in declaration order.
    /// </summary>
    internal static IReadOnlyList<string> ValuesOf(string schemaName)
    {
        JsonElement schema = Document.Value.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty(schemaName);

        return [.. schema.GetProperty("enum").EnumerateArray().Select(static value => value.GetString()!)];
    }
}
```

The snapshot must sit next to the test assembly. Add to `tests/SYT.RozetkaPay.Tests/SYT.RozetkaPay.Tests.csproj`:

```xml
  <ItemGroup>
    <None Include="../../src/SYT.RozetkaPay/docs/openapi.json" CopyToOutputDirectory="PreserveNewest" Link="openapi.json" />
  </ItemGroup>
```

- [ ] **Step 2: Write the failing enum test**

Create `tests/SYT.RozetkaPay.Tests/EnumWireTokenTests.cs`:

```csharp
using System.Text.Json;
using SYT.RozetkaPay.Models.AlternativePayments;
using SYT.RozetkaPay.Models.Batch;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.PayParts;
using SYT.RozetkaPay.Serialization;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// What matters on the wire is the token, not the member name. These tests serialize through the
/// SDK's own options, so they fail if either the converter or an annotation drifts.
/// </summary>
public class EnumWireTokenTests
{
    private static readonly JsonSerializerOptions Options = SdkSerializerOptions.Value;

    public static TheoryData<string, Type> SpecBackedEnums => new()
    {
        { "CustomerCheckoutLocale", typeof(CustomerCheckoutLocale) },
        { "AlternativePaymentProvider", typeof(AlternativePaymentProvider) },
        { "PayPartsPaymentMode", typeof(PayPartsPaymentMode) },
        { "BatchPaymentMode", typeof(BatchPaymentMode) },
        { "OperationType", typeof(OperationType) },
    };

    [Theory]
    [MemberData(nameof(SpecBackedEnums))]
    public void Enum_ShouldSerializeToExactlyTheTokensTheSpecDeclares(string schemaName, Type enumType)
    {
        HashSet<string> expected = [.. SpecEnumSource.ValuesOf(schemaName)];

        HashSet<string> actual = [.. Enum.GetValues(enumType)
            .Cast<object>()
            .Select(member => JsonSerializer.Serialize(member, enumType, Options).Trim('"'))];

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Locale_ShouldSerializeUppercase()
    {
        // The README quick start uses this exact value, and it was going out as "uk".
        Assert.Equal("\"UK\"", JsonSerializer.Serialize(CustomerCheckoutLocale.UK, Options));
    }

    [Fact]
    public void Provider_ShouldSerializeWithoutAnInventedWordBreak()
    {
        Assert.Equal("\"leaselink\"", JsonSerializer.Serialize(AlternativePaymentProvider.LeaseLink, Options));
    }
}
```

Add `using SYT.RozetkaPay.Serialization;` — `SdkSerializerOptions.Value` comes from Task 2.

- [ ] **Step 3: Run and confirm it fails**

Run: `dotnet test --filter "FullyQualifiedName~EnumWireTokenTests" -f net10.0`
Expected: FAIL for every theory row — the converter lowercases and snake-cases everything.

- [ ] **Step 4: Drop the naming policy from the enum converter**

In `src/SYT.RozetkaPay/Serialization/SdkSerializerOptions.cs`, in the converter list, change:

```csharp
                new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower),
```

to:

```csharp
                // No naming policy: every member carries its own [JsonStringEnumMemberName] with the
                // exact token the OpenAPI document declares. A policy here would silently re-derive
                // those tokens from member names, which is how "UK" became "uk" and "leaselink"
                // became "lease_link".
                new System.Text.Json.Serialization.JsonStringEnumConverter(),
```

- [ ] **Step 5: Annotate every member of the five spec-backed enums**

For each enum in the theory data, read the declared values with:

```bash
cd /Volumes/T9/Projects/RozetkaPay
python3 -c "
import json
s=json.load(open('src/SYT.RozetkaPay/docs/openapi.json'))['components']['schemas']
for n in ['CustomerCheckoutLocale','AlternativePaymentProvider','PayPartsPaymentMode','BatchPaymentMode','OperationType']:
    print(n, s[n]['enum'])
"
```

Then make each SDK enum match that set exactly — add missing members, **delete wrong ones** — and annotate:

```csharp
using System.Text.Json.Serialization;

/// <summary>Checkout locale presented to the payer.</summary>
public enum CustomerCheckoutLocale
{
    /// <summary>Ukrainian.</summary>
    [JsonStringEnumMemberName("UK")]
    UK,

    /// <summary>English.</summary>
    [JsonStringEnumMemberName("EN")]
    EN,

    /// <summary>Polish.</summary>
    [JsonStringEnumMemberName("PL")]
    PL,
}
```

`ES`, `FR`, `SK`, `DE` are not in the spec — delete them.

`PayPartsPaymentMode` is the largest change: `Single` and `Installment` are not spec values at all. Replace the whole enum with `Hosted` (`"hosted"`) and `Direct` (`"direct"`), then fix every compile error the deletion produces.

- [ ] **Step 6: Run the enum test and confirm it passes**

Run: `dotnet test --filter "FullyQualifiedName~EnumWireTokenTests" -f net10.0`
Expected: PASS

- [ ] **Step 7: Extend the same treatment to `ResponseCode` and `SubscriptionCallbackType`**

These two are large (`ResponseCode`: 57 SDK members against 185 spec values) and are response-side, so a wrong member surfaces as a `JsonException` on an otherwise successful call. Generate the member list rather than typing it:

```bash
python3 -c "
import json,re
s=json.load(open('src/SYT.RozetkaPay/docs/openapi.json'))['components']['schemas']
for v in s['ResponseCode']['enum']:
    name=re.sub(r'[^0-9a-zA-Z]+',' ',v).title().replace(' ','')
    if name[0].isdigit(): name='Code'+name
    print(f'    /// <summary>Provider response code <c>{v}</c>.</summary>')
    print(f'    [JsonStringEnumMemberName(\"{v}\")]')
    print(f'    {name},')
    print()
"
```

Add these two to the `SpecBackedEnums` theory data and re-run.

- [ ] **Step 8: Fix the README example**

`src/SYT.RozetkaPay/README.md:119-140` uses `CustomerCheckoutLocale.UK`. The member survives, so the snippet still compiles — confirm it now documents the right wire value if it mentions one.

- [ ] **Step 9: Full suite**

Run: `dotnet test -c Release`
Expected: 0 failed. Expect fallout in fixture-based tests that hard-coded `"single"` or `"uk"` — those fixtures were wrong; update them.

- [ ] **Step 10: Commit**

```bash
git checkout -b fix/EXP-384-enum-wire-tokens
git add src/SYT.RozetkaPay/ tests/SYT.RozetkaPay.Tests/
git commit -m "EXP-384 fix(json): serialize enums to the published tokens"
```

---

## Task 4: Remove the 404 fallback and make retries operation-aware

**Ticket:** [EXP-385](https://experthub.youtrack.cloud/issue/EXP-385) (W3) · Branch `fix/EXP-385-no-hidden-fallback`

**Files:**
- Modify: `src/SYT.RozetkaPay/Services/BaseService.cs:299-353` (`GetAsyncWithFallback` pair), `:407-468` (`PostAsyncWithFallback` pair), `:534-598` (`PostAsyncWithNoContentWithFallback` pair), `:815-855` (`ExecuteWithRetryAsync`)
- Modify: `src/SYT.RozetkaPay/Services/AlternativePaymentService.cs:71-97`
- Modify: `src/SYT.RozetkaPay/Services/PayPartsService.cs:73-142`
- Modify: `src/SYT.RozetkaPay/Services/CustomerService.cs:229-240`
- Test: `tests/SYT.RozetkaPay.Tests/CriticalFixesTests.cs` (extend)

**Interfaces:**
- Consumes: nothing from Tasks 1-3.
- Produces: `BaseService.ExecuteWithRetryAsync` gains a required `bool isIdempotent` parameter. Every derived service calling it must pass it. The three `*WithFallback` helper families are **deleted** — later tasks must not call them.

**Two changes, one ticket:** they are inseparable. The fallback is the mechanism by which a non-idempotent POST gets repeated, so removing it and gating retries are the same fix seen from two ends.

- [ ] **Step 1: Write the failing test — a 404 must not become a second request**

Add to `tests/SYT.RozetkaPay.Tests/CriticalFixesTests.cs`:

```csharp
    [Fact]
    public async Task NotFound_ShouldNotBeRetriedOnAnotherRoute()
    {
        List<string> targets = [];

        StubHttpMessageHandler handler = new((request, _) =>
        {
            targets.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"code\":\"order_not_found\"}"),
            });
        });

        using HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.rozetkapay.com") };
        PayPartsService service = new(TestConfiguration(), httpClient);

        await Assert.ThrowsAsync<RozetkaPayNotFoundException>(
            () => service.CreateOrderAsync(ValidPayPartsOrder()));

        // One route, one attempt. A missing order must not be reinterpreted as a missing endpoint.
        Assert.Single(targets);
    }
```

Reuse whatever `TestConfiguration()` / fixture helpers already exist in that file; `rg -n "private static RozetkaPayConfiguration" tests/` to find them.

- [ ] **Step 2: Run and confirm it fails**

Run: `dotnet test --filter "NotFound_ShouldNotBeRetriedOnAnotherRoute" -f net10.0`
Expected: FAIL — `targets` has two entries.

- [ ] **Step 3: Delete the fallback helpers**

Remove all six methods from `BaseService`: `GetAsyncWithFallback` (both overloads), `PostAsyncWithFallback` (both), `PostAsyncWithNoContentWithFallback` (both).

- [ ] **Step 4: Point each caller at its official route only**

For each call site, the official route is the one present in the live document; the fallback argument was the legacy route. Confirm which is which before deleting:

```bash
python3 -c "
import json
s=json.load(open('src/SYT.RozetkaPay/docs/openapi.json'))
print('\n'.join(sorted(s['paths'])))
" | rg "payparts|alternative|wallet"
```

Then rewrite each call to the plain helper with the official route. The legacy routes are handled separately in [EXP-403](https://experthub.youtrack.cloud/issue/EXP-403) — do not preserve them here.

- [ ] **Step 5: Run and confirm the fallback test passes**

Run: `dotnet test --filter "NotFound_ShouldNotBeRetriedOnAnotherRoute" -f net10.0`
Expected: PASS

- [ ] **Step 6: Write the failing idempotency test**

```csharp
    [Fact]
    public async Task MutatingOperation_ShouldNotBeRetried_WhenTheServerFails()
    {
        int attempts = 0;

        StubHttpMessageHandler handler = new((_, _) =>
        {
            attempts++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("{}"),
            });
        });

        RozetkaPayConfiguration config = TestConfiguration();
        config.RetryPolicy = RetryPolicy.Standard;

        using HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.rozetkapay.com") };
        PaymentService service = new(config, httpClient);

        await Assert.ThrowsAsync<RozetkaPayException>(() => service.ConfirmPaymentAsync(ValidConfirmRequest()));

        // A 503 can arrive after the gateway already accepted the confirmation. Repeating it is a
        // second financial mutation, and the spec makes no at-most-once promise for confirm.
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ReadOperation_ShouldStillBeRetried()
    {
        int attempts = 0;

        StubHttpMessageHandler handler = new((_, _) =>
        {
            attempts++;
            return Task.FromResult(attempts < 3
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("{}") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        });

        RozetkaPayConfiguration config = TestConfiguration();
        config.RetryPolicy = RetryPolicy.Standard;
        config.RetryPolicy.BaseDelay = TimeSpan.Zero;

        using HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.rozetkapay.com") };
        PaymentService service = new(config, httpClient);

        await service.GetInfoAsync("external-id");

        Assert.Equal(3, attempts);
    }
```

- [ ] **Step 7: Run and confirm the first fails, the second passes**

Run: `dotnet test --filter "FullyQualifiedName~CriticalFixesTests" -f net10.0`
Expected: `MutatingOperation_ShouldNotBeRetried_WhenTheServerFails` FAILS with `attempts == 3`.

- [ ] **Step 8: Gate the retry loop on idempotency**

Change the signature in `BaseService`:

```csharp
    /// <param name="isIdempotent">
    /// Whether repeating this operation is safe. A read is. A mutation is not, unless the provider
    /// documents an at-most-once guarantee for it: creating a payment qualifies, because the API
    /// promises at most one success per <c>external_id</c>, while confirm, cancel and refund carry
    /// no such promise and must reach the caller as a single attempt.
    /// </param>
    protected async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        bool isIdempotent,
        CancellationToken cancellationToken = default)
```

and in the loop, before the existing policy check:

```csharp
            catch (Exception failure) when (isIdempotent &&
                ShouldRetryFailure(failure, retryPolicy, retryCount, cancellationToken))
```

Thread the flag out through every transport helper: `GetAsync` and `DeleteAsync` pass `true`; `PostAsync`, `PatchAsync`, `PostAsyncWithNoContent`, `PostWithoutBodyAsync` take the flag from their own callers and default it to `false`.

Then set it per operation in the thirteen services: `true` only for create-payment (spec-guaranteed at-most-once by `external_id`) and reads.

- [ ] **Step 9: Run the full suite**

Run: `dotnet test -c Release`
Expected: 0 failed.

- [ ] **Step 10: Commit**

```bash
git checkout -b fix/EXP-385-no-hidden-fallback
git add src/SYT.RozetkaPay/ tests/SYT.RozetkaPay.Tests/
git commit -m "EXP-385 fix(reliability): drop the 404 fallback and gate retries on idempotency"
```

---

## Task 5: Partial capture and cancel

**Ticket:** [EXP-386](https://experthub.youtrack.cloud/issue/EXP-386) (W4) · Branch `fix/EXP-386-partial-capture`

**Files:**
- Modify: `src/SYT.RozetkaPay/Models/Payments/PaymentModels.cs:73-114` (`ConfirmPaymentRequest`, `CancelPaymentRequest`)
- Test: `tests/SYT.RozetkaPay.Tests/PartialCaptureContractTests.cs`

**Interfaces:**
- Consumes: `Product` from `SYT.RozetkaPay.Models.Common`, already used by `CreatePaymentRequest.Products`; `SdkSerializerOptions.Value` from Task 2.
- Produces: `ConfirmPaymentRequest` and `CancelPaymentRequest` gain `Amount` (`decimal?`), `Currency` (`string?`), `Products` (`List<Product>?`), `Payload` (`string?`); `CancelPaymentRequest` additionally gains `CallbackUrl` (`string?`) and **loses** `Reason`.

- [ ] **Step 1: Read the authoritative field list**

```bash
cd /Volumes/T9/Projects/RozetkaPay
python3 -c "
import json
c=json.load(open('src/SYT.RozetkaPay/docs/openapi.json'))['components']['requestBodies']
for n in ['ConfirmPaymentRequest','CancelPaymentRequest']:
    s=c[n]['content']['application/json']['schema']
    print(n, 'required=', s.get('required'))
    for k,v in s['properties'].items():
        print('   ', k, v.get('type'), v.get('format') or '', v.get('\$ref') or '')
"
```

Use that output — not this plan — as the field list. The plan may be stale; the snapshot is checked against the live document by Task 6.

- [ ] **Step 2: Write the failing test**

Create `tests/SYT.RozetkaPay.Tests/PartialCaptureContractTests.cs`:

```csharp
using System.Text.Json;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Serialization;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Partial capture and partial cancel are published operations. Before this contract was fixed the
/// SDK could express neither, because the request bodies carried only an identifier.
/// </summary>
public class PartialCaptureContractTests
{
    [Fact]
    public void ConfirmPaymentRequest_ShouldCarryAPartialAmount()
    {
        ConfirmPaymentRequest request = new()
        {
            ExternalId = "order-1",
            Amount = 12.34m,
            Currency = "UAH",
            Products = [new Product { Id = "sku-1", Name = "Item", Quantity = 1, Price = 12.34m }],
        };

        string json = JsonSerializer.Serialize(request, SdkSerializerOptions.Value);

        Assert.Contains("\"amount\":12.34", json);
        Assert.Contains("\"currency\":\"UAH\"", json);
        Assert.Contains("\"products\"", json);
    }

    [Fact]
    public void CancelPaymentRequest_ShouldCarryAPartialAmount()
    {
        CancelPaymentRequest request = new()
        {
            ExternalId = "order-1",
            Amount = 5m,
            Currency = "UAH",
        };

        string json = JsonSerializer.Serialize(request, SdkSerializerOptions.Value);

        Assert.Contains("\"amount\":5", json);
        Assert.Contains("\"currency\":\"UAH\"", json);
    }

    [Fact]
    public void CancelPaymentRequest_ShouldNotCarryAnInventedReasonField()
    {
        // 'reason' is not in the published schema. Sending it invited callers to depend on a field
        // the gateway ignores.
        Assert.Null(typeof(CancelPaymentRequest).GetProperty("Reason"));
    }
}
```

- [ ] **Step 3: Run and confirm it fails**

Run: `dotnet test --filter "FullyQualifiedName~PartialCaptureContractTests" -f net10.0`
Expected: FAIL — the properties do not exist.

- [ ] **Step 4: Add the fields and delete `Reason`**

Edit both classes in `src/SYT.RozetkaPay/Models/Payments/PaymentModels.cs` to match the schema from Step 1, with XML docs on every new member. Do **not** add `[Required]` — attribute alignment is [EXP-402](https://experthub.youtrack.cloud/issue/EXP-402), and mixing it in here would make two tickets edit the same lines.

- [ ] **Step 5: Run and confirm it passes**

Run: `dotnet test --filter "FullyQualifiedName~PartialCaptureContractTests" -f net10.0`
Expected: PASS

- [ ] **Step 6: Full suite, then commit**

```bash
dotnet test -c Release
git checkout -b fix/EXP-386-partial-capture
git add src/SYT.RozetkaPay/Models/Payments/PaymentModels.cs tests/SYT.RozetkaPay.Tests/PartialCaptureContractTests.cs
git commit -m "EXP-386 fix(api): let confirm and cancel carry a partial amount"
```

---

## Task 6: Make the live document the CI oracle

**Ticket:** [EXP-387](https://experthub.youtrack.cloud/issue/EXP-387) (W5) · Branch `test/EXP-387-live-spec-oracle`

**Files:**
- Create: `scripts/verify-openapi-drift.sh`
- Modify: `.github/workflows/ci.yml`
- Modify: `src/SYT.RozetkaPay/docs/openapi.json` (refresh to the live document)

**Interfaces:**
- Consumes: nothing.
- Produces: `scripts/verify-openapi-drift.sh`, exit 0 when the snapshot matches the live document semantically, exit 1 otherwise. Task 3's `SpecEnumSource` reads its expected enum tokens out of that snapshot — this job is what keeps those expectations honest.

**Why this lands mid-plan, not last:** it is the guard the whole contract block relies on. Landing it before Tasks 9-19 means each contract PR is checked against a snapshot that is provably current.

- [ ] **Step 1: Write the drift script**

Create `scripts/verify-openapi-drift.sh`, `chmod +x`:

```bash
#!/usr/bin/env bash
#
# Compares the committed OpenAPI snapshot against the document RozetkaPay publishes.
#
# The comparison is semantic, not textual: key order and whitespace are not contract, so a
# byte diff would cry wolf on a reformat and teach everyone to ignore it. What is contract is
# the set of operations, the shape of every schema, and the exact enum tokens.
set -euo pipefail

LIVE_URL="${ROZETKAPAY_OPENAPI_URL:-https://docs.rozetkapay.com/openapi.json}"
SNAPSHOT="$(dirname "$0")/../src/SYT.RozetkaPay/docs/openapi.json"
LIVE="$(mktemp)"
trap 'rm -f "$LIVE"' EXIT

echo "Fetching $LIVE_URL"
curl --fail --silent --show-error --location --max-time 60 -o "$LIVE" "$LIVE_URL"

python3 - "$SNAPSHOT" "$LIVE" <<'PY'
import json, sys

snapshot, live = (json.load(open(p)) for p in sys.argv[1:3])

def canonical(doc):
    return json.dumps(doc, sort_keys=True, ensure_ascii=False, indent=1).splitlines()

def operations(doc):
    return {
        (method.upper(), path, op.get("operationId"))
        for path, item in doc.get("paths", {}).items()
        for method, op in item.items()
        if method in ("get", "post", "put", "patch", "delete", "head", "options")
    }

problems = []

added = operations(live) - operations(snapshot)
removed = operations(snapshot) - operations(live)
for op in sorted(added):
    problems.append(f"operation added upstream, absent from snapshot: {op}")
for op in sorted(removed):
    problems.append(f"operation in snapshot, absent upstream: {op}")

import difflib
diff = list(difflib.unified_diff(canonical(snapshot), canonical(live),
                                 "snapshot", "live", n=2, lineterm=""))
if diff:
    problems.append(f"schema drift, {len(diff)} diff lines:")
    problems.extend(diff[:200])

if problems:
    print("OpenAPI drift detected.\n")
    print("\n".join(problems))
    print("\nRefresh the snapshot and reconcile the SDK, then re-run.")
    sys.exit(1)

print(f"OpenAPI snapshot matches the live document: "
      f"{len(live.get('paths', {}))} paths, {len(operations(live))} operations.")
PY
```

- [ ] **Step 2: Run it and watch it fail on today's drift**

Run: `./scripts/verify-openapi-drift.sh`
Expected: FAIL, reporting the `Metadata` schema additions. That failure is the script working.

- [ ] **Step 3: Prove it passes when the snapshot is current**

```bash
curl -fsSL https://docs.rozetkapay.com/openapi.json -o src/SYT.RozetkaPay/docs/openapi.json
./scripts/verify-openapi-drift.sh
```
Expected: PASS, reporting `59 paths, 67 operations`.

- [ ] **Step 4: Prove it fails on an injected change — do not skip this**

```bash
python3 -c "
import json
p='src/SYT.RozetkaPay/docs/openapi.json'
d=json.load(open(p)); d['components']['schemas']['Metadata']['maxProperties']=99
json.dump(d,open(p,'w'))
"
./scripts/verify-openapi-drift.sh; echo "exit=$?"
```
Expected: exit 1. Then restore with the curl from Step 3. A gate nobody has seen fail is not a gate.

- [ ] **Step 5: Wire it into CI**

In `.github/workflows/ci.yml`, add a job that does not block the build on an upstream outage but does surface drift:

```yaml
  openapi-drift:
    name: OpenAPI drift
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - name: Compare the snapshot against the published document
        run: ./scripts/verify-openapi-drift.sh
```

- [ ] **Step 6: Commit**

```bash
git checkout -b test/EXP-387-live-spec-oracle
git add scripts/verify-openapi-drift.sh .github/workflows/ci.yml src/SYT.RozetkaPay/docs/openapi.json
git commit -m "EXP-387 test(openapi): check the snapshot against the published document"
```

---

## Task 7: Stop mutating a consumer's HttpClient

**Ticket:** [EXP-388](https://experthub.youtrack.cloud/issue/EXP-388) (W6) · Branch `fix/EXP-388-http-client-ownership`

**Files:**
- Modify: `src/SYT.RozetkaPay/Services/BaseService.cs:122-142` (constructor), and every `SendAsync` call site
- Test: `tests/SYT.RozetkaPay.Tests/ConsumerHttpClientOwnershipTests.cs`

**Interfaces:**
- Consumes: `ExecuteWithRetryAsync(operation, isIdempotent, ct)` from Task 4.
- Produces: `BaseService` gains `private readonly Uri _baseAddress` and `private readonly TimeSpan _timeout`; request targets become absolute.

- [ ] **Step 1: Write the failing test**

Create `tests/SYT.RozetkaPay.Tests/ConsumerHttpClientOwnershipTests.cs`:

```csharp
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// The SDK accepts a caller-supplied HttpClient. Writing to it is not the SDK's to do: the client
/// may be pooled by IHttpClientFactory and shared with the rest of the application, and once it has
/// served a request the runtime forbids the write outright.
/// </summary>
public class ConsumerHttpClientOwnershipTests
{
    [Fact]
    public void Construction_ShouldNotRewriteTheCallersBaseAddressOrTimeout()
    {
        StubHttpMessageHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}"),
            }));

        using HttpClient consumerClient = new(handler)
        {
            BaseAddress = new Uri("https://consumer.example/"),
            Timeout = TimeSpan.FromSeconds(5),
        };

        using RozetkaPayClient client = new(TestConfiguration(), consumerClient);

        Assert.Equal(new Uri("https://consumer.example/"), consumerClient.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(5), consumerClient.Timeout);
    }

    [Fact]
    public async Task Construction_ShouldSucceedOnAClientThatHasAlreadySentARequest()
    {
        StubHttpMessageHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}"),
            }));

        using HttpClient consumerClient = new(handler);
        await consumerClient.GetAsync("https://consumer.example/warmup");

        // Before the fix this threw InvalidOperationException: "This instance has already started
        // one or more requests."
        using RozetkaPayClient client = new(TestConfiguration(), consumerClient);
        Assert.NotNull(client);
    }

    private static RozetkaPayConfiguration TestConfiguration() => new()
    {
        Login = "test-login",
        Password = "test-password",
        BaseUrl = "https://api.rozetkapay.com",
        Timeout = TimeSpan.FromSeconds(30),
    };
}
```

- [ ] **Step 2: Run and confirm both fail**

Run: `dotnet test --filter "FullyQualifiedName~ConsumerHttpClientOwnershipTests" -f net10.0`
Expected: first FAILS on the rewritten `BaseAddress`; second FAILS with `InvalidOperationException`.

- [ ] **Step 3: Snapshot the endpoint instead of writing it**

In the `BaseService` constructor, replace lines 128-130:

```csharp
        // Endpoint and timeout only. Header state belongs to the request, below.
        HttpClient.BaseAddress = new Uri(Configuration.BaseUrl);
        HttpClient.Timeout = Configuration.Timeout;
```

with:

```csharp
        // Snapshotted, never written to the client. The client may be owned by the consumer and
        // pooled by IHttpClientFactory, and HttpClient forbids both writes once it has served a
        // request - so the previous form silently reconfigured someone else's client at best, and
        // threw at worst. Each request carries its own absolute target and its own timeout.
        _baseAddress = new Uri(Configuration.BaseUrl);
        _timeout = Configuration.Timeout;
```

with the matching fields declared next to `HttpClient`.

- [ ] **Step 4: Build absolute targets and apply the timeout per request**

Add to `BaseService`:

```csharp
    /// <summary>
    /// Resolve a request target against the configured endpoint without consulting the client.
    /// </summary>
    private Uri ResolveTarget(string endpoint) => new(_baseAddress, endpoint);

    /// <summary>
    /// A token that trips at the configured timeout, linked to the caller's own token so that
    /// cancellation still belongs to the caller and a timeout is still distinguishable from it.
    /// </summary>
    private CancellationTokenSource CreateTimeoutScope(CancellationToken cancellationToken)
    {
        CancellationTokenSource scope = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        scope.CancelAfter(_timeout);
        return scope;
    }
```

Change `CreateAuthenticatedRequest` to `new(method, ResolveTarget(endpoint))`, and wrap each `SendAsync` in the timeout scope, passing `scope.Token`.

- [ ] **Step 5: Run and confirm both pass**

Run: `dotnet test --filter "FullyQualifiedName~ConsumerHttpClientOwnershipTests" -f net10.0`
Expected: PASS

- [ ] **Step 6: Confirm timeout and cancellation stay distinguishable**

Run: `dotnet test --filter "FullyQualifiedName~PreDispatchCancellation" -f net10.0`
Expected: PASS. These tests own the cancellation contract; if they fail, the linked-token wiring is wrong — a caller's cancellation must still surface as the caller's own token.

- [ ] **Step 7: Full suite, then commit**

```bash
dotnet test -c Release
git checkout -b fix/EXP-388-http-client-ownership
git add src/SYT.RozetkaPay/Services/BaseService.cs tests/SYT.RozetkaPay.Tests/ConsumerHttpClientOwnershipTests.cs
git commit -m "EXP-388 fix(http): stop writing to a client the SDK does not own"
```

---

## Task 8: Stop labelling local time as UTC

**Ticket:** [EXP-390](https://experthub.youtrack.cloud/issue/EXP-390) (W8) · Branch `fix/EXP-390-datetime-utc`

**Files:**
- Modify: `src/SYT.RozetkaPay/Converters/FlexibleDateTimeConverter.cs:57-60`, `:72-76`
- Test: `tests/SYT.RozetkaPay.Tests/DateTimeConversionTests.cs`

**Interfaces:**
- Consumes: `SdkSerializerOptions.Value` from Task 2.
- Produces: no signature change. `Write` now converts; `Read` of a Unix timestamp now returns `DateTimeKind.Utc`.

**Scope note:** the ticket also proposes moving the public surface to `DateTimeOffset`. That is a seventy-property change across every model and belongs with the contract block, not here. This task fixes the converter — the part that is silently wrong today. Record the `DateTimeOffset` migration as a follow-up on EXP-390 before closing it.

- [ ] **Step 1: Write the failing tests**

Create `tests/SYT.RozetkaPay.Tests/DateTimeConversionTests.cs`:

```csharp
using System.Text.Json;
using SYT.RozetkaPay.Serialization;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// The converter writes an ISO-8601 string ending in 'Z'. That suffix is a claim about the instant,
/// and it has to be true.
/// </summary>
public class DateTimeConversionTests
{
    [Fact]
    public void Write_ShouldConvertLocalTimeToUtcRatherThanRelabelIt()
    {
        // Built as a real local time so the assertion holds in any timezone the suite runs in,
        // including UTC, where it degenerates to the already-correct case.
        DateTime local = new DateTime(2026, 7, 29, 12, 0, 0, DateTimeKind.Local);
        string expected = local.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z";

        string json = JsonSerializer.Serialize(local, SdkSerializerOptions.Value);

        Assert.Equal($"\"{expected}\"", json);
    }

    [Fact]
    public void Write_ShouldLeaveUtcUnchanged()
    {
        DateTime utc = new DateTime(2026, 7, 29, 10, 0, 0, DateTimeKind.Utc);

        string json = JsonSerializer.Serialize(utc, SdkSerializerOptions.Value);

        Assert.Equal("\"2026-07-29T10:00:00.000Z\"", json);
    }

    [Fact]
    public void Read_ShouldReturnUtcForAUnixTimestamp()
    {
        DateTime parsed = JsonSerializer.Deserialize<DateTime>("0", SdkSerializerOptions.Value);

        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), parsed);
    }

    [Fact]
    public void Read_ShouldNormalizeAnOffsetToUtc()
    {
        DateTime parsed = JsonSerializer.Deserialize<DateTime>(
            "\"2026-07-29T12:00:00+02:00\"", SdkSerializerOptions.Value);

        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
        Assert.Equal(new DateTime(2026, 7, 29, 10, 0, 0, DateTimeKind.Utc), parsed);
    }
}
```

- [ ] **Step 2: Run and confirm the local and Unix cases fail**

Run: `dotnet test --filter "FullyQualifiedName~DateTimeConversionTests" -f net10.0`
Expected: `Write_ShouldConvertLocalTimeToUtcRatherThanRelabelIt` FAILS unless the machine is on UTC — run it forced: `TZ=Europe/Berlin dotnet test --filter "FullyQualifiedName~DateTimeConversionTests" -f net10.0`. `Read_ShouldReturnUtcForAUnixTimestamp` FAILS on `Kind`.

- [ ] **Step 3: Fix `Write`**

Replace lines 72-76 of `FlexibleDateTimeConverter.cs`:

```csharp
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // The trailing 'Z' asserts UTC, so the value has to actually be UTC. Relabelling a local
        // time shifted the instant by the machine's offset and called the result universal - a bug
        // that is invisible on a UTC build agent and wrong everywhere else.
        //
        // Unspecified is treated as already-UTC rather than as local: the API only ever emits UTC,
        // so guessing 'local' would corrupt values that round-tripped through a Kind-losing layer.
        DateTime utc = value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => value,
        };

        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
    }
```

- [ ] **Step 4: Fix the Unix timestamp path**

Replace line 60:

```csharp
                return DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
```

with:

```csharp
                // .DateTime yields Kind=Unspecified, which then serializes back out as if it were
                // UTC without ever having been converted. .UtcDateTime states what it is.
                return DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
```

- [ ] **Step 5: Run and confirm all four pass, in a non-UTC zone**

Run: `TZ=Europe/Berlin dotnet test --filter "FullyQualifiedName~DateTimeConversionTests" -f net10.0`
Expected: PASS. Then `TZ=UTC dotnet test --filter "FullyQualifiedName~DateTimeConversionTests" -f net10.0` — also PASS.

- [ ] **Step 6: Full suite, then commit**

```bash
dotnet test -c Release
git checkout -b fix/EXP-390-datetime-utc
git add src/SYT.RozetkaPay/Converters/FlexibleDateTimeConverter.cs tests/SYT.RozetkaPay.Tests/DateTimeConversionTests.cs
git commit -m "EXP-390 fix(time): convert to UTC instead of relabelling local time"
```

---

## Remaining work: Tasks 9-25

The contract block is mechanically uniform — read the schema, reshape the model, assert the whole body — so it is tracked in the tickets rather than expanded here. Each ticket carries its own field list, evidence and DoD. Expand a ticket into steps at the moment you pick it up, using Task 4 as the template.

**Order and rationale:**

| Order | Tickets | Why here |
|---|---|---|
| 9-19 | [EXP-391](https://experthub.youtrack.cloud/issue/EXP-391) … [EXP-401](https://experthub.youtrack.cloud/issue/EXP-401) | Contract. Runs after Task 6 so every PR is checked against a snapshot proven current. EXP-400 (name collisions) may spawn new tickets — link them to it. |
| 20 | [EXP-402](https://experthub.youtrack.cloud/issue/EXP-402) | Validation. Must follow the contract block: turning validation on over today's `[Required]` markings would reject valid requests, because 16 of them contradict the spec. |
| 21 | [EXP-403](https://experthub.youtrack.cloud/issue/EXP-403) | The 16 off-spec routes. Blocked on RozetkaPay confirming each one. |
| 22 | [EXP-404](https://experthub.youtrack.cloud/issue/EXP-404) | Public API baseline — only meaningful once the surface has stopped moving. |
| 23 | [EXP-405](https://experthub.youtrack.cloud/issue/EXP-405) | Release cross-channel checksum. Independent; may be pulled forward if convenient. |
| 24 | [EXP-406](https://experthub.youtrack.cloud/issue/EXP-406) | Dependency refresh. Independent. |
| 25 | [EXP-407](https://experthub.youtrack.cloud/issue/EXP-407) | Documentation, strictly last — it may only describe what the tests prove. |

**Before starting the contract block,** re-run the parity harness: Tasks 1-8 will have closed some of its findings, and working from a stale report wastes effort. The harness is a reflection dump of the built assembly compared against the live document; rebuild it from the method recorded in §2 of the spec.

---

## Codex review protocol

Every PR gets an independent review before merge. Run it against the pushed branch:

```bash
cd /Volumes/T9/Projects/RozetkaPay
codex exec --sandbox read-only --skip-git-repo-check "$(cat <<'EOF'
Review the diff of the current branch against main. This is a payment SDK; the contract it
must satisfy is https://docs.rozetkapay.com/openapi.json, not the repository's own docs,
which have been wrong before.

Judge only what the diff changes. For each concern give file:line, why it is wrong, and what
a caller would observe. Verify claims by reading the code or running it — do not speculate.
State plainly whether the change is safe to merge.
EOF
)"
```

Treat the output as a reviewer's opinion, not a verdict: confirm each point against the code before acting on it. Codex was right about the redirect leak and the enum tokens, and wrong about at least one thing already.

## Definition of done for this plan

- Tasks 1-8 merged, each with an independent review.
- `dotnet test -c Release` green on `net9.0` and `net10.0`.
- `./scripts/verify-openapi-drift.sh` green, **and** observed to fail on an injected change.
- No `[Obsolete]` shims added to preserve compatibility that nobody needs.
- Tickets EXP-383…EXP-390 closed with the PR link and the evidence each DoD asks for.
