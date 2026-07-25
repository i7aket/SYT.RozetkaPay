using System.Net;
using System.Text.Json;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Deterministic contract coverage for every operation the pinned RozetkaPay OpenAPI document publishes.
/// </summary>
/// <remarks>
/// <para>
/// Two independent oracles are compared here. The first is
/// <c>src/SYT.RozetkaPay/docs/openapi.json</c>, the snapshot EXP-354 pinned and this suite only reads. The
/// second is <see cref="OpenApiOperationManifest"/>, a hand-written table of what the SDK must send. Neither
/// is derived from the other, and neither is derived from the production routing constants, so a wrong verb,
/// a wrong route, a missing operation, or a legacy member standing in for a published one fails rather than
/// being confirmed.
/// </para>
/// <para>
/// Every row then invokes its canonical SDK method for real over
/// <see cref="ContractRecordingHandler"/> and the request that comes out is asserted against the row's
/// literal expectation. Nothing in this file can reach the network: the configured base address is in the
/// reserved <c>.invalid</c> TLD, the transport never forwards a request, and every row asserts the host it
/// observed.
/// </para>
/// <para>
/// This suite proves SDK-to-pinned-contract coverage for all 67 operations. It makes no claim that a live
/// RozetkaPay environment has answered them - see <see cref="SandboxSmokeTests"/> for the separate,
/// opt-in, read-only live check.
/// </para>
/// </remarks>
public class OpenApiOperationContractTests
{
    /// <summary>Operation count the pinned document declares, as <c>OpenApi59OperationTests</c> also pins.</summary>
    private const int PinnedOperationCount = 67;

    /// <summary>
    /// Content type a JSON operation must send, including the charset the SDK's <c>StringContent</c> adds.
    /// </summary>
    private const string ExpectedJsonContentType = "application/json; charset=utf-8";

    /// <summary>Bound on a single contract invocation. Nothing here waits on I/O, so this is generous.</summary>
    private static readonly TimeSpan InvocationTimeout = TimeSpan.FromSeconds(30);

    private static readonly string[] HttpVerbs =
        ["get", "put", "post", "delete", "patch", "head", "options", "trace"];

    /// <summary>
    /// One case per published operation. The operationId is the theory argument - it is stable, readable in
    /// test output, and asserted unique - and the row itself is looked up from it.
    /// </summary>
    public static TheoryData<string> OperationIds
    {
        get
        {
            TheoryData<string> data = [];
            foreach (OpenApiOperationContract contract in OpenApiOperationManifest.All)
            {
                data.Add(contract.OperationId);
            }

            return data;
        }
    }

    // ===================== Document-versus-manifest set guard =====================

    /// <summary>
    /// The manifest and the pinned document must describe the same set of operations, compared on the full
    /// <c>(method, path template, operationId)</c> identity. Anything the provider adds, removes, renames,
    /// or moves to another verb shows up here as a named row rather than as silence.
    /// </summary>
    [Fact]
    public void Manifest_ShouldCoverExactlyTheOperationsThePinnedDocumentDeclares()
    {
        HashSet<OperationIdentity> document = DocumentIdentities();
        HashSet<OperationIdentity> manifest = ManifestIdentities();

        Assert.Equal(PinnedOperationCount, document.Count);
        Assert.Equal(PinnedOperationCount, manifest.Count);

        // Rendered as a single actionable message: both directions of the difference at once, so a drift
        // report does not have to be reconstructed from one failure at a time.
        (List<OperationIdentity> missing, List<OperationIdentity> unexpected) = Diff(document, manifest);
        Assert.True(
            missing.Count == 0 && unexpected.Count == 0,
            Describe(missing, unexpected));
    }

    /// <summary>
    /// The manifest must hold exactly 67 rows, with no operationId and no identity appearing twice. A
    /// duplicated row would let one operation stand in for another while the set comparison still passed.
    /// </summary>
    [Fact]
    public void Manifest_ShouldDeclareEachOperationExactlyOnce()
    {
        IReadOnlyList<OpenApiOperationContract> rows = OpenApiOperationManifest.All;

        Assert.Equal(PinnedOperationCount, rows.Count);

        string[] duplicateOperationIds = rows
            .GroupBy(static row => row.OperationId, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();
        Assert.Empty(duplicateOperationIds);

        // Distinct identities and distinct rows: 67 unique tuples, and therefore 67 unique operations.
        Assert.Equal(PinnedOperationCount, rows.Select(static row => row.Identity).Distinct().Count());
    }

    /// <summary>
    /// Group sizes match section 6 of the EXP-337 plan and sum to the pinned operation count, so a row
    /// added to one group and dropped from another cannot cancel out.
    /// </summary>
    [Fact]
    public void Manifest_GroupSizes_ShouldMatchTheCoverageGroupsAndSumToTheOperationCount()
    {
        Dictionary<string, int> actual = OpenApiOperationManifest.All
            .GroupBy(static row => row.Group, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);

        Assert.Equal(
            OpenApiOperationManifest.ExpectedGroupSizes.OrderBy(static entry => entry.Key, StringComparer.Ordinal),
            actual.OrderBy(static entry => entry.Key, StringComparer.Ordinal));
        Assert.Equal(PinnedOperationCount, OpenApiOperationManifest.ExpectedGroupSizes.Values.Sum());
    }

    /// <summary>
    /// Each row's request-body policy must be the one the pinned document declares, so that "this operation
    /// sends JSON" and "this operation sends nothing" are read out of the contract rather than asserted from
    /// whatever the SDK happens to do.
    /// </summary>
    /// <remarks>
    /// Without this the <see cref="ContractBodyPolicy"/> column would be a hand-written claim that no oracle
    /// checks: a row could declare <see cref="ContractBodyPolicy.None"/> for an operation the provider
    /// declares a body for, and the executable assertion would then happily confirm the SDK sending none.
    /// </remarks>
    [Fact]
    public void ManifestBodyPolicy_ShouldMatchWhatThePinnedDocumentDeclares()
    {
        using JsonDocument document = LoadSnapshot();
        Dictionary<OperationIdentity, JsonElement> operations = DocumentOperations(document);

        foreach (OpenApiOperationContract row in OpenApiOperationManifest.All)
        {
            JsonElement operation = operations[Identity(row)];
            ContractBodyPolicy declared = operation.TryGetProperty("requestBody", out _)
                ? ContractBodyPolicy.Json
                : ContractBodyPolicy.None;

            Assert.True(
                declared == row.Body,
                $"{row}: the document declares body policy {declared} but the manifest row declares {row.Body}.");
        }

        // The bodyless POST the provider publishes is a real shape, not a transcription slip: pinning it here
        // keeps a future "every POST has a body" simplification from passing.
        Assert.Equal(ContractBodyPolicy.None, Row("getInStorePaymentInfo").Body);
        Assert.Equal("POST", Row("getInStorePaymentInfo").Method);
    }

    /// <summary>
    /// Each row's authentication policy must be the one the pinned document declares. The document states
    /// authentication globally and overrides it exactly once, with an empty operation-level security list.
    /// </summary>
    [Fact]
    public void ManifestAuthPolicy_ShouldMatchWhatThePinnedDocumentDeclares()
    {
        using JsonDocument document = LoadSnapshot();
        Dictionary<OperationIdentity, JsonElement> operations = DocumentOperations(document);

        foreach (OpenApiOperationContract row in OpenApiOperationManifest.All)
        {
            JsonElement operation = operations[Identity(row)];

            // An empty "security" array is the OpenAPI spelling of "this operation overrides the global
            // requirement and needs no credential". Anything else inherits the global Basic requirement.
            ContractAuthPolicy declared =
                operation.TryGetProperty("security", out JsonElement security)
                && security.ValueKind == JsonValueKind.Array
                && security.GetArrayLength() == 0
                    ? ContractAuthPolicy.Anonymous
                    : ContractAuthPolicy.Authenticated;

            Assert.True(
                declared == row.Auth,
                $"{row}: the document declares auth policy {declared} but the manifest row declares {row.Auth}.");
        }
    }

    /// <summary>
    /// Every row names a real method on the canonical SDK interface a consumer injects. This is the guard
    /// against a row whose delegate quietly calls something other than the interface it advertises.
    /// </summary>
    [Theory]
    [MemberData(nameof(OperationIds))]
    public void Row_ShouldNameAnExistingMethodOnItsCanonicalServiceInterface(string operationId)
    {
        OpenApiOperationContract row = Row(operationId);

        Assert.True(row.ServiceInterface.IsInterface, $"{row}: the canonical service must be an interface.");
        Assert.Contains(row.ServiceInterface.GetMethods(), method => method.Name == row.ServiceMethod);
    }

    /// <summary>
    /// No row may be satisfied by a member the SDK has marked obsolete. Those members exist for source
    /// compatibility and call legacy routes the official document does not publish; counting one as
    /// coverage would report an operation as reachable while the published one stayed untested.
    /// </summary>
    /// <remarks>
    /// The wire assertions would already catch a legacy member by its different request target. This is the
    /// cheaper, more direct statement of the same rule, and it names the offending member instead of showing
    /// an unexpected path.
    /// </remarks>
    [Theory]
    [MemberData(nameof(OperationIds))]
    public void Row_ShouldNotBeSatisfiedByAnObsoleteMember(string operationId)
    {
        OpenApiOperationContract row = Row(operationId);

        // Every overload of the named method, so a row cannot point at a name whose canonical spelling is
        // fine while the overload it actually uses is deprecated.
        foreach (System.Reflection.MethodInfo method in row.ServiceInterface
            .GetMethods()
            .Where(method => method.Name == row.ServiceMethod))
        {
            Assert.False(
                method.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: true).Length > 0,
                $"{row}: {row.ServiceInterface.Name}.{row.ServiceMethod} is obsolete and cannot be canonical coverage.");
        }
    }

    /// <summary>
    /// The pinned document must give every operation a non-empty operationId, because operationId is part of
    /// the identity the two oracles are compared on. A blank one would silently collapse rows together.
    /// </summary>
    [Fact]
    public void PinnedDocument_ShouldGiveEveryOperationANonEmptyOperationId()
    {
        using JsonDocument document = LoadSnapshot();
        int operations = 0;

        foreach (JsonProperty path in document.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (JsonProperty operation in path.Value.EnumerateObject())
            {
                if (!HttpVerbs.Contains(operation.Name, StringComparer.Ordinal))
                {
                    continue;
                }

                operations++;
                Assert.True(
                    operation.Value.TryGetProperty("operationId", out JsonElement operationId),
                    $"{operation.Name.ToUpperInvariant()} {path.Name} must declare an operationId.");
                Assert.False(
                    string.IsNullOrWhiteSpace(operationId.GetString()),
                    $"{operation.Name.ToUpperInvariant()} {path.Name} must declare a non-empty operationId.");
            }
        }

        Assert.Equal(PinnedOperationCount, operations);
    }

    // ===================== Guard-detects-drift meta-tests =====================

    /// <summary>
    /// Proves the set comparison actually detects a missing row. Without this, an exact-set assertion that
    /// silently compared the manifest to itself would look identical to a working one.
    /// </summary>
    [Fact]
    public void SetComparison_ShouldReportARowMissingFromTheManifest()
    {
        HashSet<OperationIdentity> document = DocumentIdentities();
        HashSet<OperationIdentity> manifest = ManifestIdentities();

        OperationIdentity dropped = manifest.Single(identity => identity.OperationId == "validateMerchantKeys");
        manifest.Remove(dropped);

        (List<OperationIdentity> missing, List<OperationIdentity> unexpected) = Diff(document, manifest);

        Assert.Equal([dropped], missing);
        Assert.Empty(unexpected);
        Assert.Contains("validateMerchantKeys", Describe(missing, unexpected), StringComparison.Ordinal);
    }

    /// <summary>
    /// Proves the comparison detects a row the document does not publish - the shape a legacy SDK route
    /// would take if it were counted as official coverage.
    /// </summary>
    [Fact]
    public void SetComparison_ShouldReportARowThePinnedDocumentDoesNotDeclare()
    {
        HashSet<OperationIdentity> document = DocumentIdentities();
        HashSet<OperationIdentity> manifest = ManifestIdentities();

        OperationIdentity legacy = new("POST", "/api/payouts/v1/new", "legacyCreatePayout");
        manifest.Add(legacy);

        (List<OperationIdentity> missing, List<OperationIdentity> unexpected) = Diff(document, manifest);

        Assert.Empty(missing);
        Assert.Equal([legacy], unexpected);
        Assert.Contains("legacyCreatePayout", Describe(missing, unexpected), StringComparison.Ordinal);
    }

    /// <summary>
    /// Proves the comparison detects a renamed operation as both a missing and an unexpected row, rather
    /// than as a matching count.
    /// </summary>
    [Fact]
    public void SetComparison_ShouldReportARenamedOperationInBothDirections()
    {
        HashSet<OperationIdentity> document = DocumentIdentities();
        HashSet<OperationIdentity> manifest = ManifestIdentities();

        OperationIdentity original = manifest.Single(identity => identity.OperationId == "getP2PLimits");
        manifest.Remove(original);
        manifest.Add(original with { OperationId = "getP2PLimitsRenamed" });

        (List<OperationIdentity> missing, List<OperationIdentity> unexpected) = Diff(document, manifest);

        // Counts still match; only the identity comparison catches this.
        Assert.Equal(document.Count, manifest.Count);
        Assert.Equal([original], missing);
        Assert.Single(unexpected);
        Assert.Equal("getP2PLimitsRenamed", unexpected[0].OperationId);
    }

    /// <summary>
    /// Proves a duplicated row fails the once-only assertion. A <see cref="HashSet{T}"/> comparison alone
    /// would collapse the duplicate and pass, which is why row counting is a separate assertion.
    /// </summary>
    [Fact]
    public void RowCounting_ShouldRejectADuplicatedManifestRow()
    {
        List<OpenApiOperationContract> rows = [.. OpenApiOperationManifest.All];
        rows.Add(rows[0]);

        Assert.Equal(PinnedOperationCount + 1, rows.Count);

        // The duplicate is visible by operationId and by identity, and invisible to the set comparison.
        Assert.Contains(
            rows.GroupBy(static row => row.OperationId, StringComparer.Ordinal),
            static group => group.Count() > 1);
        Assert.Equal(PinnedOperationCount, rows.Select(static row => row.Identity).Distinct().Count());
    }

    // ===================== Executable per-operation contract =====================

    /// <summary>
    /// Invokes the canonical SDK method of one operation and asserts the exact request it produced: verb,
    /// concrete path and query, body policy, content type, body sentinels, and authentication policy.
    /// </summary>
    [Theory]
    [MemberData(nameof(OperationIds))]
    public async Task Operation_ShouldSendExactlyTheRequestThePinnedDocumentDeclares(string operationId)
    {
        OpenApiOperationContract row = Row(operationId);

        using ContractRecordingHandler handler = new(row.Response, ContractServiceHost.ExpectedBasicCredentials);
        using ContractServiceHost host = new(handler);
        using CancellationTokenSource timeout = new(InvocationTimeout);

        await AssertExpectedOutcomeAsync(row, host, timeout.Token);

        // Exactly one request: no retry, no probe, and no legacy-route fallback behind the caller's back.
        ContractRequest request = Assert.Single(handler.Requests);

        AssertRequestTarget(row, request);
        AssertBody(row, request);
        AssertAuthentication(row, request);
    }

    /// <summary>
    /// The caller's cancellation token must reach the transport for every operation, so a consumer can
    /// actually abandon any of the 67 calls. Cancelling from inside the handler is observable only if the
    /// token that arrived there is the caller's own.
    /// </summary>
    [Theory]
    [MemberData(nameof(OperationIds))]
    public async Task Operation_ShouldPropagateTheCallersCancellationTokenToTheTransport(string operationId)
    {
        OpenApiOperationContract row = Row(operationId);

        using ContractRecordingHandler handler = new(row.Response, ContractServiceHost.ExpectedBasicCredentials);
        using ContractServiceHost host = new(handler);
        using CancellationTokenSource cancellation = new(InvocationTimeout);

        handler.OnRequest = cancellation.Cancel;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => row.InvokeAsync(host, cancellation.Token));

        ContractRequest request = Assert.Single(handler.Requests);
        Assert.True(
            request.CancellationObserved,
            $"{row}: the caller's cancellation token must reach the transport.");
    }

    /// <summary>
    /// The optional partnership and customer headers are sent only when they are configured. Sending them
    /// unconditionally - even empty - would change how the provider authorizes a request.
    /// </summary>
    [Theory]
    [MemberData(nameof(OperationIds))]
    public async Task Operation_ShouldOmitTheOptionalHeaders_WhenTheyAreNotConfigured(string operationId)
    {
        OpenApiOperationContract row = Row(operationId);

        using ContractRecordingHandler handler = new(row.Response, ContractServiceHost.ExpectedBasicCredentials);
        using ContractServiceHost host = new(
            handler,
            ContractServiceHost.CreateConfigurationWithoutOptionalHeaders());
        using CancellationTokenSource timeout = new(InvocationTimeout);

        await AssertExpectedOutcomeAsync(row, host, timeout.Token);

        ContractRequest request = Assert.Single(handler.Requests);

        Assert.DoesNotContain("X-ON-BEHALF-OF", request.CredentialHeaderNames, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-CUSTOMER-AUTH", request.CredentialHeaderNames, StringComparer.OrdinalIgnoreCase);

        // Basic auth is not optional, so an authenticated operation still carries it.
        if (row.Auth == ContractAuthPolicy.Authenticated)
        {
            Assert.Equal("Basic", request.AuthorizationScheme);
        }
        else
        {
            Assert.Empty(request.CredentialHeaderNames);
        }
    }

    /// <summary>
    /// The one operation the document declares anonymous must reach the provider with no credential at all,
    /// must send no content, and must return the <c>Location</c> of the <c>302</c> without ever requesting
    /// the target the provider named.
    /// </summary>
    [Fact]
    public async Task DeclineOperation_ShouldBeAnonymousAndReturnTheLocationWithoutFollowingIt()
    {
        OpenApiOperationContract row = Row("declinePaymentInstruction");

        Assert.Equal(ContractAuthPolicy.Anonymous, row.Auth);
        Assert.Equal(ContractResponseKind.Redirect, row.Response);

        using ContractRecordingHandler handler = new(row.Response, ContractServiceHost.ExpectedBasicCredentials);
        using ContractServiceHost host = new(handler);
        using CancellationTokenSource timeout = new(InvocationTimeout);

        await row.InvokeAsync(host, timeout.Token);

        // One request only: the redirect target - a different absolute URI - was never fetched.
        ContractRequest request = Assert.Single(handler.Requests);
        Assert.Equal(ContractServiceHost.Host, request.RequestUri.Host);
        Assert.NotEqual(
            new Uri(ContractRecordingHandler.RedirectLocation).Host,
            request.RequestUri.Host);

        // None of the four credential-bearing headers, not just Authorization.
        Assert.Empty(request.CredentialHeaderNames);
        Assert.Null(request.AuthorizationScheme);
        Assert.False(request.CarriesExpectedBasicCredentials);
        Assert.False(request.HasContent);
    }

    /// <summary>
    /// Exactly one operation is anonymous, and it is the decline operation the document declares that way.
    /// Every other row is authenticated.
    /// </summary>
    [Fact]
    public void Manifest_ShouldDeclareExactlyOneAnonymousOperation()
    {
        OpenApiOperationContract[] anonymous = OpenApiOperationManifest.All
            .Where(static row => row.Auth == ContractAuthPolicy.Anonymous)
            .ToArray();

        OpenApiOperationContract single = Assert.Single(anonymous);
        Assert.Equal("declinePaymentInstruction", single.OperationId);
        Assert.Equal(PinnedOperationCount - 1, OpenApiOperationManifest.All
            .Count(static row => row.Auth == ContractAuthPolicy.Authenticated));
    }

    /// <summary>
    /// Every row targets the unroutable contract host. Together with a transport that never forwards, this
    /// is what makes the deterministic layer independent of the network.
    /// </summary>
    [Theory]
    [MemberData(nameof(OperationIds))]
    public async Task Operation_ShouldNotBeAbleToReachTheNetwork(string operationId)
    {
        OpenApiOperationContract row = Row(operationId);

        using ContractRecordingHandler handler = new(row.Response, ContractServiceHost.ExpectedBasicCredentials);
        using ContractServiceHost host = new(handler);
        using CancellationTokenSource timeout = new(InvocationTimeout);

        await AssertExpectedOutcomeAsync(row, host, timeout.Token);

        ContractRequest request = Assert.Single(handler.Requests);

        // The reserved .invalid TLD can never resolve, so even a regression that bypassed this handler
        // could not reach RozetkaPay - or any other host.
        Assert.Equal(ContractServiceHost.Host, request.RequestUri.Host);
        Assert.EndsWith(".invalid", request.RequestUri.Host, StringComparison.Ordinal);
        Assert.Equal(Uri.UriSchemeHttps, request.RequestUri.Scheme);
    }

    // ===================== Assertion helpers =====================

    /// <summary>
    /// Verb, absolute path, query keys, full request target, and an empty fragment - all against the row's
    /// own literal, never against a value the production URL helper produced.
    /// </summary>
    private static void AssertRequestTarget(OpenApiOperationContract row, ContractRequest request)
    {
        Assert.Equal(row.Method, request.Method.Method);
        Assert.Equal(row.ExpectedAbsolutePath, request.RequestUri.AbsolutePath);

        string[] actualQueryKeys = request.RequestUri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(static pair => pair.Split('=', 2)[0])
            .ToArray();
        Assert.Equal(row.ExpectedQueryKeys, actualQueryKeys);

        // The primary assertion: the whole concrete request target, escaped exactly once.
        Assert.Equal(row.ExpectedPathAndQuery, request.RequestUri.PathAndQuery);

        // A fragment is never part of an API request target, and would not be sent to the provider.
        Assert.Equal(string.Empty, request.RequestUri.Fragment);
    }

    /// <summary>
    /// Body policy, content type, and the row's unique sentinels. A bodyless operation must carry no
    /// content object at all rather than an invented empty JSON document.
    /// </summary>
    private static void AssertBody(OpenApiOperationContract row, ContractRequest request)
    {
        if (row.Body == ContractBodyPolicy.None)
        {
            Assert.False(request.HasContent, $"{row}: the operation declares no request body.");
            Assert.Null(request.Body);
            Assert.Null(request.ContentType);
            Assert.Empty(row.ExpectedBodyFragments);
            return;
        }

        Assert.True(request.HasContent, $"{row}: the operation declares an application/json request body.");
        Assert.Equal(ExpectedJsonContentType, request.ContentType);
        Assert.NotNull(request.Body);

        // Sentinels are unique per operation, so a row cannot pass on another row's payload. Exact JSON
        // shapes stay the property of the serializer-focused suites.
        Assert.NotEmpty(row.ExpectedBodyFragments);
        foreach (string fragment in row.ExpectedBodyFragments)
        {
            Assert.Contains(fragment, request.Body, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Authentication policy: Basic that decodes to exactly the placeholder pair for an authenticated
    /// operation, and not one credential-bearing header for the anonymous one.
    /// </summary>
    private static void AssertAuthentication(OpenApiOperationContract row, ContractRequest request)
    {
        if (row.Auth == ContractAuthPolicy.Anonymous)
        {
            Assert.Empty(request.CredentialHeaderNames);
            Assert.Null(request.AuthorizationScheme);
            Assert.False(request.CarriesExpectedBasicCredentials);
            return;
        }

        Assert.Equal("Basic", request.AuthorizationScheme);

        // Decoded inside the transport and reported as a boolean: the credential itself never reaches an
        // assertion message, and this proves UTF-8 "login:password" with exactly one separating colon.
        Assert.True(
            request.CarriesExpectedBasicCredentials,
            $"{row}: Authorization must be Basic and decode to the placeholder login:password pair.");

        Assert.Equal(row.ExpectedCredentialHeaders.Order(StringComparer.Ordinal),
            request.CredentialHeaderNames.Order(StringComparer.Ordinal));

        // Never in the URI: a credential in a query string would be logged by every intermediary.
        Assert.DoesNotContain(
            ContractServiceHost.PasswordPlaceholder,
            request.RequestUri.ToString(),
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            ContractServiceHost.LoginPlaceholder,
            request.RequestUri.ToString(),
            StringComparison.OrdinalIgnoreCase);

        // The user agent identifies the SDK on every authenticated call.
        Assert.Equal([ContractServiceHost.UserAgentPlaceholder], request.SafeHeaderValues("User-Agent"));
    }

    /// <summary>
    /// Invoke the row and assert the outcome its controlled response implies: the redirect row succeeds, and
    /// every other row surfaces the SDK's mapped validation failure for the deterministic <c>400</c>.
    /// </summary>
    /// <remarks>
    /// A <c>400</c> keeps this suite from having to duplicate the success DTO of all 67 operations, and -
    /// unlike a <c>404</c> - never triggers the legacy-route fallbacks several services still carry, so the
    /// "exactly one request" assertion stays meaningful.
    /// </remarks>
    private static async Task AssertExpectedOutcomeAsync(
        OpenApiOperationContract row,
        ContractServiceHost host,
        CancellationToken cancellationToken)
    {
        if (row.Response == ContractResponseKind.Redirect)
        {
            await row.InvokeAsync(host, cancellationToken);
            return;
        }

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(
            () => row.InvokeAsync(host, cancellationToken));

        Assert.NotNull(exception.ApiError);
        Assert.Equal(HttpStatusCode.BadRequest, exception.ApiError.StatusCode);
        Assert.Equal("contract_probe_rejected", exception.ApiError.Code);

        // Neither the credential placeholders nor this row's own sentinels may appear in the message a
        // consumer would see, log, or attach to a bug report.
        AssertMessageCarriesNoSecretOrSentinel(row, exception.Message);
    }

    /// <summary>
    /// A failure message must name the operation and the status, never a credential and never a caller
    /// value. The controlled error body echoes nothing back, so a sentinel in the message would mean the
    /// SDK put the caller's own input there.
    /// </summary>
    private static void AssertMessageCarriesNoSecretOrSentinel(OpenApiOperationContract row, string message)
    {
        string[] forbidden =
        [
            ContractServiceHost.LoginPlaceholder,
            ContractServiceHost.PasswordPlaceholder,
            ContractServiceHost.OnBehalfOfPlaceholder,
            ContractServiceHost.CustomerAuthPlaceholder
        ];

        foreach (string value in forbidden)
        {
            Assert.DoesNotContain(value, message, StringComparison.OrdinalIgnoreCase);
        }

        foreach (string fragment in row.ExpectedBodyFragments)
        {
            Assert.DoesNotContain(fragment, message, StringComparison.Ordinal);
        }
    }

    // ===================== Identity plumbing =====================

    /// <summary>
    /// The tuple the two oracles are compared on. A record so that a difference renders readably and
    /// equality covers all three components.
    /// </summary>
    private sealed record OperationIdentity(string Method, string PathTemplate, string OperationId)
    {
        public override string ToString() => $"{OperationId} ({Method} {PathTemplate})";
    }

    private static OpenApiOperationContract Row(string operationId)
    {
        return Assert.Single(OpenApiOperationManifest.All, row => row.OperationId == operationId);
    }

    /// <summary>Identities the pinned document declares, read from the snapshot on every call.</summary>
    private static HashSet<OperationIdentity> DocumentIdentities()
    {
        using JsonDocument document = LoadSnapshot();
        HashSet<OperationIdentity> identities = [];

        foreach (JsonProperty path in document.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (JsonProperty operation in path.Value.EnumerateObject())
            {
                // Only standard HTTP operation keys. "parameters", "summary", "servers" and any vendor
                // extension on a path item are not operations.
                if (!HttpVerbs.Contains(operation.Name, StringComparer.Ordinal))
                {
                    continue;
                }

                string? operationId = operation.Value.TryGetProperty("operationId", out JsonElement id)
                    ? id.GetString()
                    : null;

                Assert.False(
                    string.IsNullOrWhiteSpace(operationId),
                    $"{operation.Name.ToUpperInvariant()} {path.Name} must declare a non-empty operationId.");

                Assert.True(
                    identities.Add(new OperationIdentity(operation.Name.ToUpperInvariant(), path.Name, operationId!)),
                    $"The pinned document declares {operation.Name.ToUpperInvariant()} {path.Name} twice.");
            }
        }

        return identities;
    }

    private static HashSet<OperationIdentity> ManifestIdentities()
    {
        return OpenApiOperationManifest.All.Select(Identity).ToHashSet();
    }

    private static OperationIdentity Identity(OpenApiOperationContract row)
    {
        return new OperationIdentity(row.Method, row.PathTemplate, row.OperationId);
    }

    /// <summary>
    /// The pinned document's operations, keyed by the same identity the set guard compares on, so a policy
    /// lookup cannot silently match a different operation on the same path.
    /// </summary>
    private static Dictionary<OperationIdentity, JsonElement> DocumentOperations(JsonDocument document)
    {
        Dictionary<OperationIdentity, JsonElement> operations = [];

        foreach (JsonProperty path in document.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (JsonProperty operation in path.Value.EnumerateObject())
            {
                if (!HttpVerbs.Contains(operation.Name, StringComparer.Ordinal))
                {
                    continue;
                }

                OperationIdentity identity = new(
                    operation.Name.ToUpperInvariant(),
                    path.Name,
                    operation.Value.GetProperty("operationId").GetString()!);
                operations[identity] = operation.Value;
            }
        }

        return operations;
    }

    private static (List<OperationIdentity> Missing, List<OperationIdentity> Unexpected) Diff(
        HashSet<OperationIdentity> document,
        HashSet<OperationIdentity> manifest)
    {
        List<OperationIdentity> missing = document
            .Except(manifest)
            .OrderBy(static identity => identity.OperationId, StringComparer.Ordinal)
            .ToList();
        List<OperationIdentity> unexpected = manifest
            .Except(document)
            .OrderBy(static identity => identity.OperationId, StringComparer.Ordinal)
            .ToList();

        return (missing, unexpected);
    }

    /// <summary>
    /// Actionable drift report. It names operations, verbs and paths only - there is no credential and no
    /// caller value anywhere in it.
    /// </summary>
    private static string Describe(List<OperationIdentity> missing, List<OperationIdentity> unexpected)
    {
        return
            $"The manifest must match the pinned OpenAPI document exactly.{Environment.NewLine}" +
            $"Declared by the document but missing from the manifest ({missing.Count}):{Environment.NewLine}" +
            $"{Render(missing)}{Environment.NewLine}" +
            $"Present in the manifest but not declared by the document ({unexpected.Count}):{Environment.NewLine}" +
            Render(unexpected);
    }

    private static string Render(List<OperationIdentity> identities)
    {
        return identities.Count == 0
            ? "  (none)"
            : string.Join(Environment.NewLine, identities.Select(static identity => $"  {identity}"));
    }

    private static JsonDocument LoadSnapshot()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "openapi.json");
        Assert.True(File.Exists(path), $"The pinned OpenAPI snapshot must be copied to {path}.");

        return JsonDocument.Parse(File.ReadAllBytes(path));
    }
}
