using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// One request exactly as the transport saw it. The target is kept as the handler-visible
/// <see cref="Uri"/> rather than the string a service built, so a value escaped at the wrong insertion
/// point - or escaped twice - is visible.
/// </summary>
/// <remarks>
/// Everything is snapshotted eagerly inside the handler, before the SDK disposes the request, so an
/// assertion runs against captured values rather than a live object the SDK has since released.
/// <see cref="AuthorizationScheme"/> and <see cref="AuthorizationParameter"/> are kept apart because the
/// contract has two halves: the scheme the SDK chose, and the credential it derived. Headers the SDK sets
/// on <see cref="HttpClient.DefaultRequestHeaders"/> are merged onto the request message before the
/// handler pipeline runs, so they are observable here exactly as they go on the wire.
/// </remarks>
internal sealed record RedactionRequest(
    HttpMethod Method,
    Uri RequestUri,
    string? Body,
    string? ContentType,
    bool HasContent,
    string? AuthorizationScheme,
    string? AuthorizationParameter,
    IReadOnlyDictionary<string, string[]> Headers)
{
    /// <summary>
    /// Path and query together, which is what a leak assertion about the request target cares about.
    /// </summary>
    internal string Target => RequestUri.PathAndQuery;

    /// <summary>
    /// The single value of a request header, or <see langword="null"/> when the header is absent.
    /// </summary>
    /// <remarks>
    /// A header carrying more than one value is a failure rather than a silent "pick the first": the SDK
    /// sets each of these exactly once, so two values would mean it appended instead of replacing.
    /// </remarks>
    internal string? Header(string name)
    {
        if (!Headers.TryGetValue(name, out string[]? values))
        {
            return null;
        }

        return Assert.Single(values);
    }
}

/// <summary>
/// Transport for the redaction suite. Answers from a per-attempt script and never forwards anywhere, so no
/// test here can reach RozetkaPay even if the SDK regressed; the configured base address is in the reserved
/// <c>.invalid</c> TLD as a second line of defence.
/// </summary>
internal sealed class RedactionHandler : HttpMessageHandler
{
    private readonly Func<int, HttpResponseMessage> _responses;
    private readonly List<RedactionRequest> _requests = [];

    private RedactionHandler(Func<int, HttpResponseMessage> responses)
    {
        _responses = responses;
    }

    internal IReadOnlyList<RedactionRequest> Requests => _requests;

    /// <summary>
    /// The single request of a non-fallback operation.
    /// </summary>
    internal RedactionRequest Single => Assert.Single(_requests);

    internal static RedactionHandler Json(string body = "{}")
    {
        return new RedactionHandler(_ => JsonResponse(HttpStatusCode.OK, body));
    }

    /// <summary>
    /// Answers <c>204</c> with no body at all, which is what the helpers accepting an empty response face.
    /// </summary>
    internal static RedactionHandler NoContent()
    {
        return new RedactionHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
    }

    /// <summary>
    /// The fallback path: the primary target answers <c>404</c>, the fallback target succeeds. With the
    /// default disabled retry policy that is exactly two requests, in that order.
    /// </summary>
    internal static RedactionHandler NotFoundThenJson(string body = "{}")
    {
        return new RedactionHandler(attempt => attempt == 1
            ? JsonResponse(HttpStatusCode.NotFound, LoggingRedactionContext.NotFoundBody)
            : JsonResponse(HttpStatusCode.OK, body));
    }

    internal static RedactionHandler Error(HttpStatusCode status, string body)
    {
        return new RedactionHandler(_ => JsonResponse(status, body));
    }

    /// <summary>
    /// Answers <c>302</c> the way the official decline operation does, so the returned <c>Location</c> can
    /// be asserted as returned-but-never-logged.
    /// </summary>
    internal static RedactionHandler Redirect(string location)
    {
        return new RedactionHandler(_ =>
        {
            HttpResponseMessage response = new(HttpStatusCode.Redirect);
            response.Headers.TryAddWithoutValidation("Location", location);
            return response;
        });
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        Dictionary<string, string[]> headers = request.Headers.ToDictionary(
            header => header.Key,
            header => header.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);

        _requests.Add(new RedactionRequest(
            request.Method,
            request.RequestUri!,
            body,
            request.Content?.Headers.ContentType?.ToString(),
            request.Content is not null,
            request.Headers.Authorization?.Scheme,
            request.Headers.Authorization?.Parameter,
            headers));

        return _responses(_requests.Count);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }
}

/// <summary>
/// Request body for the <see cref="BaseService"/> matrix. Its single property carries a marker that appears
/// nowhere else, so "the body reached the wire" and "the body never reached a log" are both unambiguous.
/// </summary>
internal sealed class RedactionPayload
{
    public string? Marker { get; set; }
}

/// <summary>
/// Response body for the <see cref="BaseService"/> matrix. Parameterless-constructible, so it also fits the
/// helpers that accept a <c>204</c>.
/// </summary>
internal sealed class RedactionResult
{
    public string? Outcome { get; set; }
}

/// <summary>
/// Test-only service that exposes the protected transport helpers of <see cref="BaseService"/> unchanged.
/// </summary>
/// <remarks>
/// <para>
/// The redaction contract belongs to the real helpers, so the matrix drives them directly instead of
/// re-implementing a rule the production code does not have. This probe also stands in for an
/// <b>external</b> derived service - the reason the no-label overloads have to fail closed: a third party
/// that passes a dynamic request target to a no-label helper must not thereby publish it to a log sink.
/// </para>
/// <para>
/// Every member is a one-line forward. Nothing is normalized, rewritten, or pre-escaped here, so what the
/// tests observe is the production behaviour and not this type's.
/// </para>
/// </remarks>
internal sealed class LoggingRedactionProbeService : BaseService
{
    internal LoggingRedactionProbeService(
        RozetkaPayConfiguration configuration,
        HttpClient httpClient,
        ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    // ===================== no-label (legacy) overloads =====================

    internal Task<RedactionResult> LegacyGetAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        return GetAsync<RedactionResult>(endpoint, cancellationToken);
    }

    internal Task<RedactionResult> LegacyPostAsync(
        string endpoint,
        RedactionPayload request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<RedactionPayload, RedactionResult>(endpoint, request, cancellationToken);
    }

    internal Task<RedactionResult> LegacyPostAllowingNoContentAsync(
        string endpoint,
        RedactionPayload request,
        CancellationToken cancellationToken = default)
    {
        return PostAsyncWithNoContent<RedactionPayload, RedactionResult>(endpoint, request, cancellationToken);
    }

    internal Task<RedactionResult> LegacyPatchAsync(
        string endpoint,
        RedactionPayload request,
        CancellationToken cancellationToken = default)
    {
        return PatchAsync<RedactionPayload, RedactionResult>(endpoint, request, cancellationToken);
    }

    internal Task<RedactionResult> LegacyDeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        return DeleteAsync<RedactionResult>(endpoint, cancellationToken);
    }




    // ===================== label-aware overloads =====================

    internal Task<RedactionResult> GetWithLabelAsync(
        string endpoint,
        string endpointForLogging,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<RedactionResult>(endpoint, endpointForLogging, cancellationToken);
    }

    internal Task<RedactionResult> PostWithLabelAsync(
        string endpoint,
        string endpointForLogging,
        RedactionPayload request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<RedactionPayload, RedactionResult>(
            endpoint,
            endpointForLogging,
            request,
            cancellationToken);
    }

    internal Task<RedactionResult> PostAllowingNoContentWithLabelAsync(
        string endpoint,
        string endpointForLogging,
        RedactionPayload request,
        CancellationToken cancellationToken = default)
    {
        return PostAsyncWithNoContent<RedactionPayload, RedactionResult>(
            endpoint,
            endpointForLogging,
            request,
            cancellationToken);
    }

    internal Task<RedactionResult> PatchWithLabelAsync(
        string endpoint,
        string endpointForLogging,
        RedactionPayload request,
        CancellationToken cancellationToken = default)
    {
        return PatchAsync<RedactionPayload, RedactionResult>(
            endpoint,
            endpointForLogging,
            request,
            cancellationToken);
    }

    internal Task<RedactionResult> PostWithoutBodyWithLabelAsync(
        string endpoint,
        string endpointForLogging,
        CancellationToken cancellationToken = default)
    {
        return PostWithoutBodyAsync<RedactionResult>(endpoint, endpointForLogging, cancellationToken);
    }

    internal Task<RedactionResult> DeleteWithLabelAsync(
        string endpoint,
        string endpointForLogging,
        CancellationToken cancellationToken = default)
    {
        return DeleteAsync<RedactionResult>(endpoint, endpointForLogging, cancellationToken);
    }

    internal Task<RedactionResult> DeleteWithBodyAndLabelAsync(
        string endpoint,
        string endpointForLogging,
        RedactionPayload request,
        CancellationToken cancellationToken = default)
    {
        return DeleteAsync<RedactionPayload, RedactionResult>(
            endpoint,
            endpointForLogging,
            request,
            cancellationToken);
    }



}

/// <summary>
/// Shared setup for the EXP-359 legacy log-redaction contract.
/// </summary>
internal static class LoggingRedactionContext
{
    /// <summary>
    /// Reserved TLD: even a regression that bypassed the scripted handler could not resolve this host.
    /// </summary>
    internal const string BaseUrl = "https://redaction.invalid";

    /// <summary>
    /// The label a no-label overload writes instead of the real request target.
    /// </summary>
    internal const string RedactedLabel = "[redacted]";

    /// <summary>
    /// Route the <see cref="BaseService"/> matrix addresses. Nothing about it is caller-controlled, so it
    /// may appear in a log; the marker appended to it may not.
    /// </summary>
    internal const string ProbeRoute = "/api/redaction-probe/v1/resource";

    internal const string ProbeFallbackRoute = "/api/redaction-probe/v1/legacy-resource";

    /// <summary>
    /// Static route template a caller of a label-aware overload supplies for the primary target.
    /// </summary>
    internal const string ProbeLabel = ProbeRoute + "/{resource_id}";

    /// <summary>
    /// Static route template a caller of a label-aware overload supplies for the fallback target.
    /// </summary>
    internal const string ProbeFallbackLabel = ProbeFallbackRoute + "/{resource_id}";

    /// <summary>
    /// Every character that could break out of its insertion point - space, '+', '/', '&amp;', '=', '?',
    /// '#', '%' - plus non-ASCII text, so the UTF-8 octets of a leak are covered too.
    /// </summary>
    internal const string HostileSuffixRaw = " +/&=?#% Привіт";

    /// <summary>
    /// <see cref="HostileSuffixRaw"/> percent-encoded exactly once, written as a literal so that no
    /// assertion mirrors the production encoder.
    /// </summary>
    internal const string HostileSuffixEncoded =
        "%20%2B%2F%26%3D%3F%23%25%20%D0%9F%D1%80%D0%B8%D0%B2%D1%96%D1%82";

    /// <summary>
    /// Marker naming the primary request target of the <see cref="BaseService"/> matrix.
    /// </summary>
    internal const string PrimaryRawMarker = "primary-target-must-never-be-logged-EXP359" + HostileSuffixRaw;

    internal const string PrimaryEncodedMarker =
        "primary-target-must-never-be-logged-EXP359" + HostileSuffixEncoded;

    /// <summary>
    /// A second, different marker for the fallback target, so a fallback log that quoted the wrong request
    /// is distinguishable from one that quoted no request at all.
    /// </summary>
    internal const string FallbackRawMarker = "fallback-target-must-never-be-logged-EXP359" + HostileSuffixRaw;

    internal const string FallbackEncodedMarker =
        "fallback-target-must-never-be-logged-EXP359" + HostileSuffixEncoded;

    /// <summary>
    /// Request-body marker. Bodies are never a log field, whichever helper sent them.
    /// </summary>
    internal const string RequestBodyMarker = "request-body-marker-must-never-be-logged-EXP359";

    /// <summary>
    /// Success-response marker. A response body is not a log field either.
    /// </summary>
    internal const string ResponseBodyMarker = "response-body-marker-must-never-be-logged-EXP359";

    internal const string LoginPlaceholder = "unit-test-login";

    internal const string PasswordPlaceholder = "unit-test-placeholder";

    internal const string CustomerAuthPlaceholder = "customer-auth-placeholder-not-a-real-token-EXP359";

    internal const string OnBehalfOfPlaceholder = "on-behalf-placeholder-not-a-real-value-EXP359";

    /// <summary>
    /// Production request-header name that carries <see cref="RozetkaPayConfiguration.CustomerAuth"/>.
    /// </summary>
    /// <remarks>
    /// Spelled out here rather than read from the SDK: the name is a private constant of
    /// <c>BaseService</c>, and a test that took it from production could not notice production renaming it.
    /// </remarks>
    internal const string CustomerAuthHeaderName = "X-CUSTOMER-AUTH";

    /// <summary>
    /// Production request-header name that carries <see cref="RozetkaPayConfiguration.OnBehalfOf"/>.
    /// </summary>
    internal const string OnBehalfOfHeaderName = "X-ON-BEHALF-OF";

    /// <summary>
    /// The authentication scheme the SDK sends. The credential itself is asserted by decoding what the
    /// handler received, never by recomputing it.
    /// </summary>
    internal const string BasicScheme = "Basic";

    /// <summary>
    /// Provider <c>404</c> body used by every fallback row. Deliberately carries a hostile provider message,
    /// so "a fallback happened" and "the provider text was not logged" are proven by the same request.
    /// </summary>
    internal const string NotFoundBody =
        """{"code":"not_found","message":"provider-message-must-never-be-logged-EXP359","error_id":"request-id-EXP359"}""";

    /// <summary>
    /// The provider message inside <see cref="NotFoundBody"/>.
    /// </summary>
    internal const string ProviderMessageMarker = "provider-message-must-never-be-logged-EXP359";

    internal const string ProbeCategory = "SYT.RozetkaPay.Services.LoggingRedactionProbeService";

    /// <summary>
    /// A row-specific raw marker. The prefix is made of unreserved characters only, so it percent-encodes
    /// to itself and <see cref="EncodedMarker"/> stays a hand-written spelling rather than a second
    /// implementation of the encoder.
    /// </summary>
    internal static string RawMarker(string row)
    {
        return $"{row}-must-never-be-logged-EXP359{HostileSuffixRaw}";
    }

    /// <summary>
    /// The single-pass percent-encoded spelling of <see cref="RawMarker"/>.
    /// </summary>
    internal static string EncodedMarker(string row)
    {
        return $"{row}-must-never-be-logged-EXP359{HostileSuffixEncoded}";
    }

    /// <summary>
    /// A row-specific marker for a value that travels in a JSON <b>body</b> rather than in the request
    /// target.
    /// </summary>
    /// <remarks>
    /// Deliberately unreserved ASCII only. The SDK serializer uses the default JSON encoder, which escapes
    /// non-ASCII and HTML-sensitive characters, so a hostile marker would appear in the body as
    /// <c>\uXXXX</c> escapes - and "the body still carries the caller's value byte-for-byte" would then be
    /// asserted against an escaped spelling instead of the value. Hostility belongs to the request-target
    /// markers; uniqueness is what a body marker needs.
    /// </remarks>
    internal static string BodyMarker(string row)
    {
        return $"{row}-must-never-be-logged-EXP359";
    }

    internal static RozetkaPayConfiguration Configuration()
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = BaseUrl,
            Login = LoginPlaceholder,
            Password = PasswordPlaceholder,
            RetryPolicy = RetryPolicy.None,
            UserAgent = "SYT.RozetkaPay.Tests"
        };
    }

    /// <summary>
    /// The same configuration with both optional authentication headers set, so a leak assertion can prove
    /// that a configured credential never reaches a sink either.
    /// </summary>
    internal static RozetkaPayConfiguration ConfigurationWithCredentials()
    {
        RozetkaPayConfiguration configuration = Configuration();
        configuration.CustomerAuth = CustomerAuthPlaceholder;
        configuration.OnBehalfOf = OnBehalfOfPlaceholder;
        return configuration;
    }

    internal static HttpClient Client(HttpMessageHandler handler)
    {
        return new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
    }

    internal static LoggingRedactionProbeService Probe(
        RedactionHandler handler,
        ILogger logger,
        RozetkaPayConfiguration? configuration = null)
    {
        return new LoggingRedactionProbeService(configuration ?? Configuration(), Client(handler), logger);
    }

    /// <summary>
    /// A capturing provider and the logger a directly constructed service is given, under a category that
    /// looks like the real ones.
    /// </summary>
    internal static (CapturingLoggerProvider Logs, ILogger Logger) Capture(string category = ProbeCategory)
    {
        CapturingLoggerProvider logs = new();
        return (logs, logs.CreateLogger(category));
    }
}

/// <summary>
/// Leak assertions shared by the redaction suite.
/// </summary>
internal static class LoggingRedactionAssert
{
    /// <summary>
    /// <paramref name="marker"/> reached no category, rendered message, structured value, or scope of any
    /// captured entry. The failure message names the offending entries so a regression is diagnosable
    /// without re-running under a debugger.
    /// </summary>
    internal static void NotLogged(CapturingLoggerProvider logs, string marker)
    {
        List<CapturedLogEntry> offenders = logs.Entries
            .Where(entry => entry.AllText.Any(text => text.Contains(marker, StringComparison.Ordinal)))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"'{marker}' reached a log sink through: " +
            string.Join(" | ", offenders.Select(entry => $"[{entry.Category}] {entry.Message}")));
    }

    /// <summary>
    /// Neither the raw spelling of a caller value nor its percent-encoded spelling was logged. Both are
    /// checked because a service logs the target it built, which is the encoded one.
    /// </summary>
    internal static void NotLoggedInEitherSpelling(CapturingLoggerProvider logs, string raw, string encoded)
    {
        NotLogged(logs, raw);
        NotLogged(logs, encoded);
    }

    /// <summary>
    /// The SDK wrote this exact static label.
    /// </summary>
    internal static void Logged(CapturingLoggerProvider logs, string label)
    {
        Assert.True(
            logs.AllText.Any(text => text.Contains(label, StringComparison.Ordinal)),
            $"Expected the static label '{label}' in the captured logs. Captured: " +
            string.Join(" | ", logs.Entries.Select(entry => $"[{entry.Category}] {entry.Message}")));
    }

    /// <summary>
    /// The SDK created no logging scope at all. A scope is written by a sink exactly like a state value, so
    /// "no scope" is part of the contract rather than an implementation detail.
    /// </summary>
    internal static void NoScopes(CapturingLoggerProvider logs)
    {
        Assert.All(logs.Entries, entry => Assert.Empty(entry.ScopeValues));
    }
}
