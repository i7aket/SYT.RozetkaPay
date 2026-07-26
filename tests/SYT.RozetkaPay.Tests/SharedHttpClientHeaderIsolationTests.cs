using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Extensions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// EXP-341. Authentication and the SDK-configured headers belong to the individual request, not to the
/// default header collection of an <see cref="HttpClient"/> the caller owns and may share.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RozetkaPayClient"/> builds every service over one client, and a consumer can hand the same
/// client to several services or use it for its own calls. Writing credentials onto
/// <see cref="HttpClient.DefaultRequestHeaders"/> made construction order observable, let one service's
/// configuration overwrite another's, and wrote to shared mutable state while requests were in flight.
/// </para>
/// <para>
/// Every assertion here therefore has two halves: what arrives on the request, and what the caller's own
/// default header collection looks like afterwards. The second half is compared against a full snapshot
/// taken before construction, not against "is it still non-empty".
/// </para>
/// </remarks>
public class SharedHttpClientHeaderIsolationTests
{
    /// <summary>
    /// Fake host. Every request is intercepted by a recording handler, so no test here can reach a network.
    /// </summary>
    private const string BaseUrl = "https://unit.test";

    private const string OnBehalfOfHeader = "X-ON-BEHALF-OF";

    private const string CustomerAuthHeader = "X-CUSTOMER-AUTH";

    /// <summary>
    /// A caller default the SDK never configures, so it must keep flowing untouched.
    /// </summary>
    private const string CallerTraceHeader = "X-CALLER-TRACE";

    private const string CallerTraceValue = "caller-trace-value-EXP341";

    private const string CallerBearerPlaceholder = "caller-bearer-placeholder-not-a-real-token-EXP341";

    private const string CallerUserAgent = "CallerOwnedClient/9.9";

    private const string CallerOnBehalfOf = "caller-on-behalf-placeholder-EXP341";

    private const string CallerCustomerAuth = "caller-customer-auth-placeholder-not-a-real-token-EXP341";

    private const string SuccessBody = """{"outcome":"ok"}""";

    /// <summary>
    /// Two SDK configurations that disagree about every header the SDK sets, so a value belonging to the
    /// wrong service is unmistakable.
    /// </summary>
    private static readonly ServiceProfile First = new(
        "first-login-placeholder-EXP341",
        "first-password-placeholder-not-a-real-secret-EXP341",
        "ProbeOne/1.0",
        "first-on-behalf-placeholder-EXP341",
        "first-customer-auth-placeholder-not-a-real-token-EXP341");

    private static readonly ServiceProfile Second = new(
        "second-login-placeholder-EXP341",
        "second-password-placeholder-not-a-real-secret-EXP341",
        "ProbeTwo/2.0",
        "second-on-behalf-placeholder-EXP341",
        "second-customer-auth-placeholder-not-a-real-token-EXP341");

    // ===================== construction does not touch caller defaults =====================

    /// <summary>
    /// Constructing services over a caller-owned client must leave its default header collection exactly
    /// as the caller supplied it - including the caller's own <c>Authorization</c>, user agent, and the
    /// two optional names the SDK also knows about.
    /// </summary>
    [Fact]
    public void Construction_ShouldLeaveCallerOwnedDefaultHeadersExactlyAsSupplied()
    {
        HeaderRecordingHandler handler = new();
        using HttpClient shared = CreateCallerOwnedClient(handler);

        string[] before = SnapshotDefaults(shared);

        // The snapshot really is the caller's configuration, so an "unchanged" assertion below cannot pass
        // against an empty collection.
        Assert.Equal(
            [
                $"Authorization: Bearer {CallerBearerPlaceholder}",
                $"User-Agent: {CallerUserAgent}",
                $"{CallerTraceHeader}: {CallerTraceValue}",
                $"{CustomerAuthHeader}: {CallerCustomerAuth}",
                $"{OnBehalfOfHeader}: {CallerOnBehalfOf}"
            ],
            before);

        RetryProbeService first = new(First.ToConfiguration(), shared);
        Assert.Equal(before, SnapshotDefaults(shared));

        RetryProbeService second = new(Second.ToConfiguration(), shared);
        Assert.Equal(before, SnapshotDefaults(shared));

        Assert.NotSame(first, second);
    }

    /// <summary>
    /// A user agent the header grammar rejects must still fail while the service is constructed - that has
    /// always been the behaviour - and it must fail without leaving a half-applied change behind on the
    /// caller's client.
    /// </summary>
    [Fact]
    public void AnInvalidUserAgent_ShouldStillFailAtConstructionWithoutTouchingTheCallerDefaults()
    {
        HeaderRecordingHandler handler = new();
        using HttpClient shared = CreateCallerOwnedClient(handler);

        string[] before = SnapshotDefaults(shared);

        RozetkaPayConfiguration configuration = First.ToConfiguration();
        configuration.UserAgent = "not a user agent (";

        Assert.Throws<FormatException>(() => new RetryProbeService(configuration, shared));
        Assert.Equal(before, SnapshotDefaults(shared));
    }

    /// <summary>
    /// The same contract for the optional headers. Before EXP-341 they were installed with
    /// <c>DefaultRequestHeaders.Add</c>, which validated them while the service was constructed; snapshotting
    /// must not quietly defer that to the first HTTP call.
    /// </summary>
    /// <remarks>
    /// The value carries a bare CRLF that is not a legal header continuation - the shape a header-injection
    /// attempt has - so it is rejected by the header grammar rather than by a hand-written check.
    /// </remarks>
    [Theory]
    [InlineData(OptionalHeader.OnBehalfOf)]
    [InlineData(OptionalHeader.CustomerAuth)]
    public void AnInvalidOptionalHeaderValue_ShouldFailAtConstructionWithoutTouchingTheCallerDefaults(
        OptionalHeader header)
    {
        const string InvalidValue = "value-EXP341\r\nX-Injected-Header: injected-value-EXP341";

        HeaderRecordingHandler handler = new();
        using HttpClient shared = CreateCallerOwnedClient(handler);

        string[] before = SnapshotDefaults(shared);

        RozetkaPayConfiguration configuration = First.ToConfiguration();
        switch (header)
        {
            case OptionalHeader.OnBehalfOf:
                configuration.OnBehalfOf = InvalidValue;
                break;
            case OptionalHeader.CustomerAuth:
                configuration.CustomerAuth = InvalidValue;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(header));
        }

        Assert.Throws<FormatException>(() => new RetryProbeService(configuration, shared));
        Assert.Equal(before, SnapshotDefaults(shared));
    }

    /// <summary>
    /// Blank stays absent: an empty or whitespace-only optional value is neither validated as a header nor
    /// sent, exactly as before.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ABlankOptionalHeaderValue_ShouldStillBeTreatedAsAbsent(string blank)
    {
        HeaderRecordingHandler handler = new();
        using HttpClient shared = new(handler) { BaseAddress = new Uri(BaseUrl) };

        RozetkaPayConfiguration configuration = First.ToConfiguration();
        configuration.OnBehalfOf = blank;
        configuration.CustomerAuth = blank;

        RetryProbeService service = new(configuration, shared);
        await service.GetJsonAsync<RetryProbeResult>("/blank-optional-headers");

        CapturedRequest request = Assert.Single(handler.Requests);
        Assert.Empty(request.Values(OnBehalfOfHeader));
        Assert.Empty(request.Values(CustomerAuthHeader));
        Assert.Empty(shared.DefaultRequestHeaders);
    }

    // ===================== per-request isolation and precedence =====================

    /// <summary>
    /// Two services over one client, called in turn: each request carries its own service's credentials
    /// and headers, nothing from the other service, and nothing appended from the caller's defaults for
    /// the names the SDK sets.
    /// </summary>
    [Fact]
    public async Task TwoServicesOverOneClient_ShouldEachSendTheirOwnRequestHeaders()
    {
        HeaderRecordingHandler handler = new();
        using HttpClient shared = CreateCallerOwnedClient(handler);
        string[] before = SnapshotDefaults(shared);

        RetryProbeService first = new(First.ToConfiguration(), shared);
        RetryProbeService second = new(Second.ToConfiguration(), shared);

        // Deliberately interleaved: on the old implementation the last constructor - and then the last
        // caller - decided what every service sent.
        await first.GetJsonAsync<RetryProbeResult>("/first-one");
        await second.GetJsonAsync<RetryProbeResult>("/second-one");
        await first.GetJsonAsync<RetryProbeResult>("/first-two");

        Assert.Equal(3, handler.Requests.Count);
        AssertBelongsTo(Single(handler, "/first-one"), First, Second);
        AssertBelongsTo(Single(handler, "/second-one"), Second, First);
        AssertBelongsTo(Single(handler, "/first-two"), First, Second);

        Assert.Equal(before, SnapshotDefaults(shared));
    }

    /// <summary>
    /// The same two services calling concurrently. Request headers are per-message state, so no attempt can
    /// observe another service's values and no attempt writes to the shared client while another is in
    /// flight.
    /// </summary>
    [Fact]
    public async Task TwoServicesOverOneClient_ShouldStayIsolatedUnderConcurrentCalls()
    {
        const int PairCount = 24;

        HeaderRecordingHandler handler = new();
        using HttpClient shared = CreateCallerOwnedClient(handler);
        string[] before = SnapshotDefaults(shared);

        RetryProbeService first = new(First.ToConfiguration(), shared);
        RetryProbeService second = new(Second.ToConfiguration(), shared);

        List<Task> calls = [];
        for (int index = 0; index < PairCount; index++)
        {
            int ordinal = index;
            calls.Add(Task.Run(() => first.GetJsonAsync<RetryProbeResult>($"/first/{ordinal}")));
            calls.Add(Task.Run(() => second.GetJsonAsync<RetryProbeResult>($"/second/{ordinal}")));
        }

        await Task.WhenAll(calls);

        Assert.Equal(PairCount * 2, handler.Requests.Count);
        foreach (CapturedRequest request in handler.Requests)
        {
            bool belongsToFirst = request.Target.StartsWith("/first/", StringComparison.Ordinal);
            AssertBelongsTo(
                request,
                belongsToFirst ? First : Second,
                belongsToFirst ? Second : First);
        }

        Assert.Equal(before, SnapshotDefaults(shared));
    }

    /// <summary>
    /// Every authenticated transport family, not just the common GET and JSON POST. This is the guard
    /// against a verb being left behind on the shared default headers.
    /// </summary>
    [Theory]
    [InlineData(TransportFamily.Get)]
    [InlineData(TransportFamily.PostJson)]
    [InlineData(TransportFamily.PostJsonAllowingNoContent)]
    [InlineData(TransportFamily.PatchJson)]
    [InlineData(TransportFamily.PostWithoutBody)]
    [InlineData(TransportFamily.Delete)]
    [InlineData(TransportFamily.DeleteWithBody)]
    public async Task EveryTransportFamily_ShouldCarryTheRequestScopedHeaders(TransportFamily family)
    {
        HeaderRecordingHandler handler = new();
        using HttpClient shared = CreateCallerOwnedClient(handler);
        string[] before = SnapshotDefaults(shared);

        RetryProbeService service = new(First.ToConfiguration(), shared);

        await InvokeAsync(service, family, "/transport");

        AssertBelongsTo(Assert.Single(handler.Requests), First, Second);
        Assert.Equal(before, SnapshotDefaults(shared));
    }

    /// <summary>
    /// A retried attempt builds a fresh request message, so the snapshotted headers have to be applied
    /// again - not carried over from the spent message and not read back off the client.
    /// </summary>
    [Fact]
    public async Task ARetriedAttempt_ShouldCarryTheSameRequestScopedHeaders()
    {
        HeaderRecordingHandler handler = new(ordinal => ordinal == 1
            ? Response(HttpStatusCode.ServiceUnavailable, """{"code":"unavailable"}""")
            : Response(HttpStatusCode.OK, SuccessBody));

        using HttpClient shared = CreateCallerOwnedClient(handler);
        string[] before = SnapshotDefaults(shared);

        RozetkaPayConfiguration configuration = First.ToConfiguration();
        configuration.RetryPolicy = new RetryPolicy
        {
            Enabled = true,
            MaxRetryAttempts = 1,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            BackoffStrategy = BackoffStrategy.Fixed
        };

        RetryProbeService service = new(configuration, shared);

        RetryProbeResult result = await service.GetJsonAsync<RetryProbeResult>("/retried");

        Assert.Equal("ok", result.Outcome);
        Assert.Equal(2, handler.Requests.Count);
        foreach (CapturedRequest request in handler.Requests)
        {
            AssertBelongsTo(request, First, Second);
        }

        Assert.Equal(before, SnapshotDefaults(shared));
    }

    /// <summary>
    /// When the SDK configuration carries no optional header, the caller's own default of that name is
    /// neither stripped from the shared client nor suppressed on the wire. The SDK does not own that
    /// collection, so it does not rewrite consumer state it was not asked to change.
    /// </summary>
    [Fact]
    public async Task AbsentOptionalConfiguration_ShouldNeitherStripNorSuppressTheCallerDefault()
    {
        HeaderRecordingHandler handler = new();
        using HttpClient shared = CreateCallerOwnedClient(handler);
        string[] before = SnapshotDefaults(shared);

        RozetkaPayConfiguration configuration = First.ToConfiguration();
        configuration.OnBehalfOf = null;
        configuration.CustomerAuth = null;

        RetryProbeService service = new(configuration, shared);
        await service.GetJsonAsync<RetryProbeResult>("/no-optional-headers");

        Assert.Equal(before, SnapshotDefaults(shared));

        CapturedRequest request = Assert.Single(handler.Requests);
        Assert.Equal([CallerOnBehalfOf], request.Values(OnBehalfOfHeader));
        Assert.Equal([CallerCustomerAuth], request.Values(CustomerAuthHeader));

        // The credentials the SDK does configure still take precedence over the caller's own.
        Assert.Equal("Basic", request.AuthorizationScheme);
        Assert.Equal(First.Credentials, request.DecodedCredentials);
        Assert.Equal([First.UserAgent], request.Values("User-Agent"));
    }

    /// <summary>
    /// The old implementation copied configuration values onto the client at construction, so later edits
    /// to the mutable configuration object could not change what a request sent. That stays true.
    /// </summary>
    [Fact]
    public async Task EditingTheConfigurationAfterConstruction_ShouldNotChangeRequestHeaders()
    {
        HeaderRecordingHandler handler = new();
        using HttpClient shared = CreateCallerOwnedClient(handler);

        RozetkaPayConfiguration configuration = First.ToConfiguration();
        RetryProbeService service = new(configuration, shared);

        configuration.Login = Second.Login;
        configuration.Password = Second.Password;
        configuration.UserAgent = Second.UserAgent;
        configuration.OnBehalfOf = Second.OnBehalfOf;
        configuration.CustomerAuth = Second.CustomerAuth;

        await service.GetJsonAsync<RetryProbeResult>("/snapshotted");

        AssertBelongsTo(Assert.Single(handler.Requests), First, Second);
    }

    // ===================== DI-resolved services =====================

    /// <summary>
    /// The DI path resolves every service over one named client that the SDK itself configures with a
    /// default user agent. The per-request user agent must win without the wire carrying both values.
    /// </summary>
    [Fact]
    public async Task DiResolvedService_ShouldSendExactlyOneCredentialAndOneUserAgent()
    {
        HeaderRecordingHandler handler = new();

        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.BaseUrl = BaseUrl;
            options.Login = First.Login;
            options.Password = First.Password;
            options.UserAgent = First.UserAgent;
            options.OnBehalfOf = First.OnBehalfOf;
            options.CustomerAuth = First.CustomerAuth;
        });
        services.AddHttpClient("RozetkaPay").ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        IPaymentService payments = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        await payments.GetInfoAsync("external-order-placeholder-EXP341");

        CapturedRequest request = Assert.Single(handler.Requests);
        Assert.Equal("Basic", request.AuthorizationScheme);
        Assert.Equal(First.Credentials, request.DecodedCredentials);
        Assert.Equal([First.UserAgent], request.Values("User-Agent"));
        Assert.Equal([First.OnBehalfOf], request.Values(OnBehalfOfHeader));
        Assert.Equal([First.CustomerAuth], request.Values(CustomerAuthHeader));
    }

    // ===================== helpers =====================

    private static Task InvokeAsync(RetryProbeService service, TransportFamily family, string endpoint)
    {
        RetryProbePayload payload = new() { Marker = "transport-marker-EXP341" };

        return family switch
        {
            TransportFamily.Get => service.GetJsonAsync<RetryProbeResult>(endpoint),
            TransportFamily.PostJson => service.PostJsonAsync<RetryProbeResult>(endpoint, payload),
            TransportFamily.PostJsonAllowingNoContent => service.PostJsonAllowingNoContentAsync(endpoint, payload),
            TransportFamily.PatchJson => service.PatchJsonAsync<RetryProbeResult>(endpoint, payload),
            TransportFamily.PostWithoutBody => service.PostWithoutBodyJsonAsync<RetryProbeResult>(endpoint),
            TransportFamily.Delete => service.DeleteJsonAsync<RetryProbeResult>(endpoint),
            TransportFamily.DeleteWithBody => service.DeleteWithBodyAsync<RetryProbeResult>(endpoint, payload),
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
    }

    private static CapturedRequest Single(HeaderRecordingHandler handler, string target)
    {
        return Assert.Single(handler.Requests, request => request.Target == target);
    }

    /// <summary>
    /// The complete per-request header contract: the expected service's credentials and headers, exactly
    /// once each, with no value belonging to the other service and no caller default appended to a name the
    /// SDK sets.
    /// </summary>
    private static void AssertBelongsTo(CapturedRequest request, ServiceProfile expected, ServiceProfile other)
    {
        Assert.Equal("Basic", request.AuthorizationScheme);
        Assert.Equal(expected.Credentials, request.DecodedCredentials);

        Assert.Equal([expected.UserAgent], request.Values("User-Agent"));
        Assert.Equal([expected.OnBehalfOf], request.Values(OnBehalfOfHeader));
        Assert.Equal([expected.CustomerAuth], request.Values(CustomerAuthHeader));

        // A header the SDK never configures still reaches the wire from the caller's defaults.
        Assert.Equal([CallerTraceValue], request.Values(CallerTraceHeader));

        string everything = string.Join(
            "\n",
            request.Headers.Select(header => $"{header.Key}: {string.Join("|", header.Value)}"));

        foreach (string foreign in new[]
                 {
                     other.UserAgent,
                     other.OnBehalfOf,
                     other.CustomerAuth,
                     CallerUserAgent,
                     CallerOnBehalfOf,
                     CallerCustomerAuth,
                     CallerBearerPlaceholder
                 })
        {
            Assert.DoesNotContain(foreign, everything, StringComparison.Ordinal);
        }

        Assert.NotEqual(other.Credentials, request.DecodedCredentials);
    }

    /// <summary>
    /// The caller's whole default header collection, rendered so that a value added, removed, reordered or
    /// rewritten is visible in one comparison.
    /// </summary>
    private static string[] SnapshotDefaults(HttpClient client)
    {
        return
        [
            .. client.DefaultRequestHeaders
                .Select(header => $"{header.Key}: {string.Join("|", header.Value)}")
                .OrderBy(static rendered => rendered, StringComparer.Ordinal)
        ];
    }

    private static HttpClient CreateCallerOwnedClient(HttpMessageHandler handler)
    {
        HttpClient client = new(handler) { BaseAddress = new Uri(BaseUrl) };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CallerBearerPlaceholder);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(CallerUserAgent);
        client.DefaultRequestHeaders.Add(OnBehalfOfHeader, CallerOnBehalfOf);
        client.DefaultRequestHeaders.Add(CustomerAuthHeader, CallerCustomerAuth);
        client.DefaultRequestHeaders.Add(CallerTraceHeader, CallerTraceValue);

        return client;
    }

    private static HttpResponseMessage Response(HttpStatusCode status, string body)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    /// <summary>
    /// Which <see cref="Services.BaseService"/> transport helper a case drives.
    /// </summary>
    public enum TransportFamily
    {
        /// <summary>Authenticated GET.</summary>
        Get,

        /// <summary>Authenticated POST carrying a JSON body.</summary>
        PostJson,

        /// <summary>Authenticated POST that also accepts <c>204</c>/empty.</summary>
        PostJsonAllowingNoContent,

        /// <summary>Authenticated PATCH carrying a JSON body.</summary>
        PatchJson,

        /// <summary>Authenticated POST with no request body at all.</summary>
        PostWithoutBody,

        /// <summary>Authenticated DELETE without a body.</summary>
        Delete,

        /// <summary>Authenticated DELETE carrying a JSON body.</summary>
        DeleteWithBody
    }

    /// <summary>
    /// Which optional SDK-configured header a case drives.
    /// </summary>
    public enum OptionalHeader
    {
        /// <summary><c>X-ON-BEHALF-OF</c>, from <see cref="RozetkaPayConfiguration.OnBehalfOf"/>.</summary>
        OnBehalfOf,

        /// <summary><c>X-CUSTOMER-AUTH</c>, from <see cref="RozetkaPayConfiguration.CustomerAuth"/>.</summary>
        CustomerAuth
    }

    /// <summary>
    /// One SDK configuration, and the exact wire values it must produce.
    /// </summary>
    private sealed record ServiceProfile(
        string Login,
        string Password,
        string UserAgent,
        string OnBehalfOf,
        string CustomerAuth)
    {
        internal string Credentials => $"{Login}:{Password}";

        internal RozetkaPayConfiguration ToConfiguration()
        {
            return new RozetkaPayConfiguration
            {
                BaseUrl = BaseUrl,
                Login = Login,
                Password = Password,
                UserAgent = UserAgent,
                OnBehalfOf = OnBehalfOf,
                CustomerAuth = CustomerAuth,
                RetryPolicy = RetryPolicy.None
            };
        }
    }

    /// <summary>
    /// One request as the handler saw it, snapshotted eagerly so that assertions never read a message the
    /// SDK has already disposed.
    /// </summary>
    private sealed record CapturedRequest(
        HttpMethod Method,
        string Target,
        string? AuthorizationScheme,
        string? AuthorizationParameter,
        IReadOnlyDictionary<string, string[]> Headers)
    {
        /// <summary>
        /// The Basic credentials as the provider would decode them, or <see langword="null"/> when the
        /// request carried no parameter. Both halves are obvious placeholders in these tests.
        /// </summary>
        internal string? DecodedCredentials => AuthorizationParameter is null
            ? null
            : Encoding.UTF8.GetString(Convert.FromBase64String(AuthorizationParameter));

        internal static CapturedRequest From(HttpRequestMessage request)
        {
            return new CapturedRequest(
                request.Method,
                request.RequestUri!.PathAndQuery,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter,
                request.Headers.ToDictionary(
                    static header => header.Key,
                    static header => header.Value.ToArray(),
                    StringComparer.OrdinalIgnoreCase));
        }

        internal string[] Values(string headerName)
        {
            return Headers.TryGetValue(headerName, out string[]? values) ? values : [];
        }
    }

    /// <summary>
    /// Recording handler that answers from a canned factory and never reaches a network. Capture is
    /// thread-safe because the isolation contract is asserted under concurrent calls.
    /// </summary>
    private sealed class HeaderRecordingHandler : HttpMessageHandler
    {
        private readonly ConcurrentQueue<CapturedRequest> _requests = new();
        private readonly Func<int, HttpResponseMessage> _responseFactory;
        private int _count;

        internal HeaderRecordingHandler(Func<int, HttpResponseMessage>? responseFactory = null)
        {
            _responseFactory = responseFactory ?? (static _ => Response(HttpStatusCode.OK, SuccessBody));
        }

        internal IReadOnlyList<CapturedRequest> Requests => [.. _requests];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            int ordinal = Interlocked.Increment(ref _count);
            _requests.Enqueue(CapturedRequest.From(request));

            return Task.FromResult(_responseFactory(ordinal));
        }
    }
}
