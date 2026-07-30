using System.Net;
using System.Reflection;
using System.Text;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.Customers;
using SYT.RozetkaPay.Models.Subscriptions;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Path-segment encoding contract (EXP-353).
///
/// Expected request targets are written as literal strings on purpose. Deriving them from
/// <see cref="Uri.EscapeDataString"/> or from the production helper would mirror the implementation
/// and would not detect escaping the wrong source value, escaping at the wrong insertion point,
/// escaping twice, or losing a segment boundary.
///
/// Every case drives a real service over a real <see cref="HttpClient"/> and asserts the request
/// target the handler actually observed, because <see cref="Uri"/> canonicalization happens between
/// the endpoint string and the handler.
/// </summary>
public class PathSegmentEncodingTests
{
    // Raw caller input -> single-pass percent-encoded segment.
    // "id +/&=?#% Привіт" => space '+' '/' '&' '=' '?' '#' '%' space + UTF-8 octets of "Привіт".
    private const string HostileRawId = "id +/&=?#% Привіт";

    private const string HostileEncodedId =
        "id%20%2B%2F%26%3D%3F%23%25%20%D0%9F%D1%80%D0%B8%D0%B2%D1%96%D1%82";

    // Two independently controlled segments. The distinct prefixes make ordering visible: a swapped
    // pair would still be correctly encoded, but would land in the wrong position.
    private const string HostileCustomerRawId = "cust +/&=?#% Привіт";

    private const string HostileCustomerEncodedId =
        "cust%20%2B%2F%26%3D%3F%23%25%20%D0%9F%D1%80%D0%B8%D0%B2%D1%96%D1%82";

    private const string HostileCardRawId = "card +/&=?#% Привіт";

    private const string HostileCardEncodedId =
        "card%20%2B%2F%26%3D%3F%23%25%20%D0%9F%D1%80%D0%B8%D0%B2%D1%96%D1%82";

    // Caller input is raw, never pre-encoded: a literal '%' becomes "%25" exactly once.
    private const string LooksEncodedRawId = "already%2Fencoded";

    private const string LooksEncodedExpected = "already%252Fencoded";

    // An identifier made only of unreserved characters must reach the wire byte-for-byte unchanged.
    private const string OrdinaryId = "plan-123";

    /// <summary>
    /// One row per caller-controlled raw path insertion. The wallet delete carries two independently
    /// controlled segments, so it appears once per parameter.
    /// </summary>
    private static readonly (string MethodKey, string ParameterName)[] PathInsertionMethods =
    {
        ("SubscriptionService.DeactivatePlanAsync", "planId"),
        ("SubscriptionService.GetPlanAsync", "planId"),
        ("SubscriptionService.UpdatePlanAsync", "planId"),
        ("SubscriptionService.DeactivateAsync", "subscriptionId"),
        ("SubscriptionService.GetAsync", "subscriptionId"),
        ("SubscriptionService.UpdateAsync", "subscriptionId"),
        ("SubscriptionService.GetPaymentsAsync", "subscriptionId"),
        ("SubscriptionService.CancelCustomerSubscriptionAsync", "subscriptionId"),
        ("SubscriptionService.CancelCustomerSubscriptionAsync/options", "subscriptionId")
    };

    // ===================== Hostile input matrix: every affected method =====================

    [Fact]
    public async Task SubscriptionService_DeactivatePlan_ShouldKeepHostilePlanIdInOneSegment()
    {
        RequestRecordingHandler handler = new();

        await PathEncodingTestContext.Subscriptions(handler).DeactivatePlanAsync(HostileRawId);

        RecordedRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, recorded.Method);
        AssertRequestTarget(recorded, $"/api/subscriptions/v1/plans/{HostileEncodedId}");
    }

    [Fact]
    public async Task SubscriptionService_GetPlan_ShouldKeepHostilePlanIdInOneSegment()
    {
        RequestRecordingHandler handler = new();

        await PathEncodingTestContext.Subscriptions(handler).GetPlanAsync(HostileRawId);

        RecordedRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, recorded.Method);
        AssertRequestTarget(recorded, $"/api/subscriptions/v1/plans/{HostileEncodedId}");
    }

    [Fact]
    public async Task SubscriptionService_UpdatePlan_ShouldKeepHostilePlanIdInOneSegmentAndKeepBody()
    {
        RequestRecordingHandler handler = new();
        UpdateSubscriptionPlanRequest request = new() { Name = "renamed", Amount = 12.34m };

        await PathEncodingTestContext.Subscriptions(handler).UpdatePlanAsync(HostileRawId, request);

        RecordedRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, recorded.Method);
        AssertRequestTarget(recorded, $"/api/subscriptions/v1/plans/{HostileEncodedId}");
        Assert.Equal("{\"name\":\"renamed\",\"amount\":12.34}", recorded.Body);
    }

    [Fact]
    public async Task SubscriptionService_Deactivate_ShouldKeepHostileSubscriptionIdInOneSegment()
    {
        RequestRecordingHandler handler = new();

        await PathEncodingTestContext.Subscriptions(handler).DeactivateAsync(HostileRawId);

        RecordedRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, recorded.Method);
        AssertRequestTarget(recorded, $"/api/subscriptions/v1/subscriptions/{HostileEncodedId}");
    }

    [Fact]
    public async Task SubscriptionService_Get_ShouldKeepHostileSubscriptionIdInOneSegment()
    {
        RequestRecordingHandler handler = new();

        await PathEncodingTestContext.Subscriptions(handler).GetAsync(HostileRawId);

        RecordedRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, recorded.Method);
        AssertRequestTarget(recorded, $"/api/subscriptions/v1/subscriptions/{HostileEncodedId}");
    }

    [Fact]
    public async Task SubscriptionService_Update_ShouldKeepHostileSubscriptionIdInOneSegmentAndKeepBody()
    {
        RequestRecordingHandler handler = new();
        UpdateSubscriptionRequest request = new() { AutoRenew = false, GiftedUntil = "2026-12-31" };

        await PathEncodingTestContext.Subscriptions(handler).UpdateAsync(HostileRawId, request);

        RecordedRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, recorded.Method);
        AssertRequestTarget(recorded, $"/api/subscriptions/v1/subscriptions/{HostileEncodedId}");
        Assert.Equal("{\"auto_renew\":false,\"gifted_until\":\"2026-12-31\"}", recorded.Body);
    }

    [Fact]
    public async Task SubscriptionService_GetPayments_ShouldKeepHostileSubscriptionIdInOneSegment()
    {
        RequestRecordingHandler handler = new();

        await PathEncodingTestContext.Subscriptions(handler).GetPaymentsAsync(HostileRawId);

        RecordedRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, recorded.Method);
        AssertRequestTarget(recorded, $"/api/subscriptions/v1/subscriptions/{HostileEncodedId}/payments");
    }

    // ===================== Dot segments =====================

    /// <summary>
    /// <see cref="Uri.EscapeDataString"/> leaves the RFC 3986 unreserved '.' unchanged and
    /// <see cref="Uri"/> removes exact dot segments before the handler runs, so "." and ".." would
    /// silently retarget the request. They are rejected instead.
    /// </summary>
    [Theory]
    [MemberData(nameof(DotSegmentCases))]
    public async Task PathIdentifier_ShouldRejectExactDotSegmentBeforeAnyRequest(
        string methodKey,
        string parameterName,
        string dotValue)
    {
        RequestRecordingHandler handler = new();

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => InvokeWithIdentifier(methodKey, dotValue, handler));

        Assert.Equal(parameterName, exception.ParamName);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [MemberData(nameof(PathIdentifierCases))]
    public async Task PathIdentifier_ShouldRejectNullBeforeAnyRequest(string methodKey, string parameterName)
    {
        RequestRecordingHandler handler = new();

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => InvokeWithIdentifier(methodKey, null!, handler));

        Assert.Equal(parameterName, exception.ParamName);
        Assert.Empty(handler.Requests);
    }


    // ===================== Raw / pre-encoded compatibility =====================

    [Fact]
    public async Task SubscriptionService_GetPlan_ShouldTreatPercentLookingInputAsRawValue()
    {
        RequestRecordingHandler handler = new();

        await PathEncodingTestContext.Subscriptions(handler).GetPlanAsync(LooksEncodedRawId);

        RecordedRequest recorded = Assert.Single(handler.Requests);
        AssertRequestTarget(recorded, $"/api/subscriptions/v1/plans/{LooksEncodedExpected}");
    }

    [Fact]
    public async Task SubscriptionService_GetPlan_ShouldMatchDocumentedIdentifierEncodingExample()
    {
        // Pins the example in the package README ("Request Identifier Encoding") to real behavior.
        RequestRecordingHandler handler = new();

        await PathEncodingTestContext.Subscriptions(handler).GetPlanAsync("plan 7/8?x=1#z");

        RecordedRequest recorded = Assert.Single(handler.Requests);
        AssertRequestTarget(recorded, "/api/subscriptions/v1/plans/plan%207%2F8%3Fx%3D1%23z");
    }

    [Fact]
    public async Task SubscriptionService_GetPlan_ShouldLeaveUnreservedIdentifierByteForByteUnchanged()
    {
        RequestRecordingHandler handler = new();

        await PathEncodingTestContext.Subscriptions(handler).GetPlanAsync(OrdinaryId);

        RecordedRequest recorded = Assert.Single(handler.Requests);
        AssertRequestTarget(recorded, $"/api/subscriptions/v1/plans/{OrdinaryId}");
    }

    // ===================== Fallback: encoded exactly once on both requests =====================





    /// <summary>
    /// Encoding must not widen the fallback trigger: only a 404 may produce a second request.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task CustomerService_GetCustomerWallet_ShouldNotFallBackOnNonNotFoundFailure(
        HttpStatusCode primaryStatus)
    {
        RequestRecordingHandler handler = new(primaryStatus, HttpStatusCode.OK);

        await Assert.ThrowsAnyAsync<RozetkaPayException>(
            () => PathEncodingTestContext.Customers(handler).GetCustomerWalletAsync(LooksEncodedRawId));

        Assert.Single(handler.Requests);
    }

    // ===================== Behavioral source audit =====================

    /// <summary>
    /// Behavioral guard against a future raw path insertion anywhere in the SDK, with no source parsing.
    ///
    /// For every public service method that accepts a string, the method is driven twice: once with a
    /// benign identifier and once with an exact dot segment. A caller-controlled value that reaches a
    /// path raw makes <see cref="Uri"/> drop a segment, so the dot run would produce a shorter path than
    /// the benign run. A value used as a query value is unaffected and compares equal.
    /// </summary>
    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public async Task PublicServiceMethods_ShouldNeverLetDotIdentifierShortenTheRequestPath(string dotValue)
    {
        List<string> shortened = new();
        List<string> skipped = new();
        int compared = 0;

        foreach (Type serviceType in PathEncodingTestContext.ConcreteServiceTypes())
        {
            foreach (MethodInfo method in PathEncodingTestContext.ProbableMethods(serviceType))
            {
                ParameterInfo[] parameters = method.GetParameters();
                for (int index = 0; index < parameters.Length; index++)
                {
                    if (parameters[index].ParameterType != typeof(string))
                    {
                        continue;
                    }

                    string label = $"{serviceType.Name}.{method.Name}({parameters[index].Name})";

                    ProbeResult benign = await PathEncodingTestContext.ProbeAsync(
                        serviceType, method, index, "benign");
                    ProbeResult probed = await PathEncodingTestContext.ProbeAsync(
                        serviceType, method, index, dotValue);

                    if (!benign.Invoked || benign.PathSegmentCounts.Count == 0)
                    {
                        // The benign run never reached the handler, so there is nothing to compare.
                        skipped.Add(label);
                        continue;
                    }

                    compared++;

                    if (!probed.Invoked || probed.PathSegmentCounts.Count == 0)
                    {
                        // Rejected before any request: the documented dot-segment contract.
                        continue;
                    }

                    if (probed.PathSegmentCounts[0] < benign.PathSegmentCounts[0])
                    {
                        shortened.Add(
                            $"{label}: benign={benign.PathAndQueries[0]} dot={probed.PathAndQueries[0]}");
                    }
                }
            }
        }

        Assert.Empty(shortened);

        // A refactor that silently stops constructing services would otherwise leave this test green
        // while auditing nothing. Every known path insertion is the floor; deriving it from the table
        // keeps the guard honest when a new insertion is added instead of pinning a stale literal.
        Assert.True(
            compared >= PathInsertionMethods.Length,
            $"Audit only compared {compared} identifier positions, expected at least "
            + $"{PathInsertionMethods.Length}. Skipped: {string.Join(", ", skipped)}");
    }

    // ===================== Data and helpers =====================

    public static IEnumerable<object[]> DotSegmentCases()
    {
        foreach ((string methodKey, string parameterName) in PathInsertionMethods)
        {
            yield return new object[] { methodKey, parameterName, "." };
            yield return new object[] { methodKey, parameterName, ".." };
        }
    }

    public static IEnumerable<object[]> PathIdentifierCases()
    {
        foreach ((string methodKey, string parameterName) in PathInsertionMethods)
        {
            yield return new object[] { methodKey, parameterName };
        }
    }

    /// <summary>
    /// Assert the complete request target a handler observed. Exact equality on
    /// <see cref="Uri.PathAndQuery"/> pins the encoding; the remaining assertions independently prove
    /// that no raw character created structure: no extra segment from '/', no query from '?', '&amp;'
    /// or '=', and no fragment from '#'.
    /// </summary>
    private static void AssertRequestTarget(RecordedRequest recorded, string expectedPath)
    {
        Uri uri = recorded.RequestUri;
        Assert.Equal(expectedPath, uri.PathAndQuery);
        Assert.Equal(expectedPath, uri.AbsolutePath);
        Assert.Equal(string.Empty, uri.Query);
        Assert.Equal(string.Empty, uri.Fragment);
        Assert.Equal(expectedPath.Split('/').Length, uri.Segments.Length);
    }

    /// <summary>
    /// Drive one public service method, placing <paramref name="value"/> in the identifier under test
    /// and a benign identifier in every other string position.
    /// </summary>
    private static Task InvokeWithIdentifier(string methodKey, string value, RequestRecordingHandler handler)
    {
        const string Other = "other-id";

        Task invocation = methodKey switch
        {
            "SubscriptionService.DeactivatePlanAsync" =>
                PathEncodingTestContext.Subscriptions(handler).DeactivatePlanAsync(value),
            "SubscriptionService.GetPlanAsync" =>
                PathEncodingTestContext.Subscriptions(handler).GetPlanAsync(value),
            "SubscriptionService.UpdatePlanAsync" =>
                PathEncodingTestContext.Subscriptions(handler)
                    .UpdatePlanAsync(value, new UpdateSubscriptionPlanRequest()),
            "SubscriptionService.DeactivateAsync" =>
                PathEncodingTestContext.Subscriptions(handler).DeactivateAsync(value),
            "SubscriptionService.GetAsync" =>
                PathEncodingTestContext.Subscriptions(handler).GetAsync(value),
            "SubscriptionService.UpdateAsync" =>
                PathEncodingTestContext.Subscriptions(handler)
                    .UpdateAsync(value, new UpdateSubscriptionRequest()),
            "SubscriptionService.GetPaymentsAsync" =>
                PathEncodingTestContext.Subscriptions(handler).GetPaymentsAsync(value),
            "SubscriptionService.CancelCustomerSubscriptionAsync" =>
                PathEncodingTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(value),
            "SubscriptionService.CancelCustomerSubscriptionAsync/options" =>
                PathEncodingTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(
                    value,
                    new CancelCustomerSubscriptionOptions { ExternalId = Other }),
            _ => throw new ArgumentOutOfRangeException(nameof(methodKey), methodKey, "Unmapped method key.")
        };

        return invocation;
    }

    // The three members below stayed on their legacy route, verb and body in EXP-355 and are
    // exercised here on purpose. The suppression is scoped to the single call each helper makes so
    // that an accidental obsolete call anywhere else still fails the build.

#pragma warning disable CS0618 // Deliberate legacy regression call.


#pragma warning restore CS0618
}

/// <summary>
/// One request as the handler observed it. The body is captured eagerly because
/// <see cref="HttpClient"/> disposes request content before the caller regains control.
/// </summary>
internal sealed record RecordedRequest(HttpMethod Method, Uri RequestUri, string? Body);

/// <summary>
/// No-network handler recording every request. Responses are served from a status sequence so a
/// primary/fallback pair can be exercised; the last entry repeats.
/// </summary>
internal sealed class RequestRecordingHandler : HttpMessageHandler
{
    private const string ErrorBody = "{\"code\":\"not_found\",\"message\":\"Resource not found\"}";

    private readonly HttpStatusCode[] _statusSequence;
    private readonly List<RecordedRequest> _requests = new();

    internal RequestRecordingHandler(params HttpStatusCode[] statusSequence)
    {
        _statusSequence = statusSequence.Length == 0
            ? new[] { HttpStatusCode.OK }
            : statusSequence;
    }

    internal IReadOnlyList<RecordedRequest> Requests => _requests;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _requests.Add(new RecordedRequest(request.Method, request.RequestUri!, body));

        HttpStatusCode status = _statusSequence[Math.Min(_requests.Count, _statusSequence.Length) - 1];
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(
                status == HttpStatusCode.OK ? "{}" : ErrorBody,
                Encoding.UTF8,
                "application/json")
        };
    }
}

/// <summary>
/// Outcome of one reflective probe of a public service method.
/// </summary>
internal sealed record ProbeResult(
    bool Invoked,
    IReadOnlyList<int> PathSegmentCounts,
    IReadOnlyList<string> PathAndQueries);

internal static class PathEncodingTestContext
{
    /// <summary>
    /// Fake host. Every request is intercepted by the recording handler, so no DNS lookup or network
    /// traffic can occur even if a test regresses.
    /// </summary>
    private const string BaseUrl = "https://unit.test";

    internal static RozetkaPayConfiguration CreateConfiguration()
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = BaseUrl,
            Login = "unit-test-login",
            Password = "unit-test-placeholder",
            RetryPolicy = RetryPolicy.None,
            UserAgent = "SYT.RozetkaPay.Tests"
        };
    }

    internal static HttpClient CreateHttpClient(RequestRecordingHandler handler)
    {
        return new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
    }

    internal static AlternativePaymentService AlternativePayments(RequestRecordingHandler handler)
    {
        return new AlternativePaymentService(CreateConfiguration(), CreateHttpClient(handler));
    }

    internal static PayPartsService PayParts(RequestRecordingHandler handler)
    {
        return new PayPartsService(CreateConfiguration(), CreateHttpClient(handler));
    }

    internal static CustomerService Customers(RequestRecordingHandler handler)
    {
        return new CustomerService(CreateConfiguration(), CreateHttpClient(handler));
    }

    internal static SubscriptionService Subscriptions(RequestRecordingHandler handler)
    {
        return new SubscriptionService(CreateConfiguration(), CreateHttpClient(handler));
    }

    /// <summary>
    /// Every concrete public service in the SDK assembly, ordered for a stable audit sequence.
    /// </summary>
    internal static IEnumerable<Type> ConcreteServiceTypes()
    {
        return typeof(BaseService).Assembly
            .GetTypes()
            .Where(type => type.IsPublic
                && type.IsClass
                && !type.IsAbstract
                && typeof(BaseService).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal);
    }

    /// <summary>
    /// Public asynchronous methods declared by a service that accept at least one string identifier.
    /// </summary>
    internal static IEnumerable<MethodInfo> ProbableMethods(Type serviceType)
    {
        return serviceType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsGenericMethodDefinition
                && typeof(Task).IsAssignableFrom(method.ReturnType)
                && method.GetParameters().Any(parameter => parameter.ParameterType == typeof(string)))
            .OrderBy(method => method.ToString(), StringComparer.Ordinal);
    }

    /// <summary>
    /// Drive one service method with <paramref name="value"/> at <paramref name="targetIndex"/> and
    /// report the request targets the handler observed. Any failure to construct arguments or any
    /// exception from the method itself yields a non-invoked result rather than failing the audit.
    /// </summary>
    internal static async Task<ProbeResult> ProbeAsync(
        Type serviceType,
        MethodInfo method,
        int targetIndex,
        string value)
    {
        RequestRecordingHandler handler = new();

        object? service;
        object?[] arguments;
        try
        {
            service = Activator.CreateInstance(
                serviceType,
                CreateConfiguration(),
                CreateHttpClient(handler),
                null);
            arguments = BuildArguments(method, targetIndex, value);
        }
        catch (Exception)
        {
            return new ProbeResult(false, Array.Empty<int>(), Array.Empty<string>());
        }

        try
        {
            if (method.Invoke(service, arguments) is Task task)
            {
                await task.ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            // The audit only cares about the request targets that reached the handler.
        }

        return new ProbeResult(
            true,
            handler.Requests.Select(request => request.RequestUri.Segments.Length).ToList(),
            handler.Requests.Select(request => request.RequestUri.PathAndQuery).ToList());
    }

    private static object?[] BuildArguments(MethodInfo method, int targetIndex, string value)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object?[] arguments = new object?[parameters.Length];

        for (int index = 0; index < parameters.Length; index++)
        {
            Type parameterType = parameters[index].ParameterType;

            if (parameterType == typeof(string))
            {
                arguments[index] = index == targetIndex ? value : "benign";
            }
            else if (parameterType == typeof(CancellationToken))
            {
                arguments[index] = CancellationToken.None;
            }
            else if (parameterType.IsValueType)
            {
                arguments[index] = Activator.CreateInstance(parameterType);
            }
            else
            {
                // Throws for a type with no accessible parameterless constructor, which the caller
                // turns into a skipped probe.
                arguments[index] = Activator.CreateInstance(parameterType, nonPublic: false);
            }
        }

        return arguments;
    }
}
