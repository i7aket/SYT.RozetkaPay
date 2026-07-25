using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// One attempt exactly as the transport saw it. Everything is snapshotted eagerly inside the handler,
/// before the SDK can dispose the request, so "the repeat was the same request" is asserted against
/// captured values rather than against a live object that the SDK has since released.
/// </summary>
/// <param name="Method">Verb on the wire.</param>
/// <param name="PathAndQuery">Concrete request target, including query values.</param>
/// <param name="BodyBytes">Request body bytes, empty when the request carried no body.</param>
/// <param name="ContentType">Rendered request content type, or <see langword="null"/> for a bodiless request.</param>
/// <param name="HasContent">Whether the request carried a body at all.</param>
/// <param name="HasAuthorization">Whether a credential-bearing header reached the transport.</param>
/// <param name="CancellationRequestedOnArrival">Whether the caller token was already cancelled.</param>
internal sealed record RetryAttempt(
    HttpMethod Method,
    string PathAndQuery,
    byte[] BodyBytes,
    string? ContentType,
    bool HasContent,
    bool HasAuthorization,
    bool CancellationRequestedOnArrival)
{
    internal string Body => Encoding.UTF8.GetString(BodyBytes);
}

/// <summary>
/// Produces the outcome of one attempt. Returning a response models an HTTP answer; throwing models a
/// transport failure. The argument is the 1-based attempt number, so a script can give every attempt its
/// own body, provider code, and request identifier.
/// </summary>
internal delegate HttpResponseMessage RetryAttemptOutcome(int attempt);

/// <summary>
/// Transport that answers from a fixed script and never forwards anywhere. Nothing in this handler opens a
/// socket, so no retry test can reach RozetkaPay even if the SDK regressed; the configured base address is
/// in the reserved <c>.invalid</c> TLD as a second line of defence.
/// </summary>
/// <remarks>
/// The script is indexed by attempt number and the last entry repeats, so "fail twice then succeed" is a
/// three-entry script and "fail on every attempt" is a one-entry script that still numbers its attempts.
/// </remarks>
internal sealed class ScriptedRetryHandler : HttpMessageHandler
{
    private readonly RetryAttemptOutcome[] _outcomes;
    private readonly List<RetryAttempt> _attempts = [];
    private readonly List<DisposalTrackingResponse> _responses = [];
    private readonly List<CancellableTrackedResponse> _cancellableResponses = [];
    private readonly List<HttpContent> _requestContents = [];
    private readonly List<DisposalTrackingContent> _requestProbes = [];

    internal ScriptedRetryHandler(params RetryAttemptOutcome[] outcomes)
    {
        if (outcomes.Length == 0)
        {
            throw new ArgumentException("A retry script needs at least one outcome.", nameof(outcomes));
        }

        _outcomes = outcomes;
    }

    /// <summary>
    /// Runs inside the handler once the attempt has been recorded and before its outcome is produced.
    /// Used to cancel the caller token while the transport is in flight.
    /// </summary>
    internal Action<int, CancellationToken>? OnAttempt { get; set; }

    /// <summary>
    /// Replaces the received request body with a disposal-tracking probe. Disposing the
    /// <see cref="HttpRequestMessage"/> disposes whatever content it owns, so a probe that comes back
    /// disposed proves the SDK released the request object it built for that attempt.
    /// </summary>
    /// <remarks>
    /// Test-only, and mutually exclusive with <see cref="RequestContents"/>: once the probe is attached the
    /// request no longer owns the original body, so that body is no longer the SDK's to dispose.
    /// </remarks>
    internal bool AttachRequestDisposalProbe { get; set; }

    internal IReadOnlyList<RetryAttempt> Attempts => _attempts;

    internal int AttemptCount => _attempts.Count;

    /// <summary>
    /// Every response handed to the SDK, kept for disposal inspection after the call returned.
    /// </summary>
    internal IReadOnlyList<DisposalTrackingResponse> Responses => _responses;

    /// <summary>
    /// Responses whose body read observes the caller's token, kept for disposal inspection.
    /// </summary>
    internal IReadOnlyList<CancellableTrackedResponse> CancellableResponses => _cancellableResponses;

    /// <summary>
    /// The request bodies the SDK built, as received. A disposed entry proves the request that owned it was
    /// disposed too.
    /// </summary>
    internal IReadOnlyList<HttpContent> RequestContents => _requestContents;

    internal IReadOnlyList<DisposalTrackingContent> RequestProbes => _requestProbes;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        int attempt = _attempts.Count + 1;

        byte[] body = request.Content is null
            ? []
            : await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

        _attempts.Add(new RetryAttempt(
            request.Method,
            request.RequestUri!.PathAndQuery,
            body,
            request.Content?.Headers.ContentType?.ToString(),
            request.Content is not null,
            request.Headers.Authorization is not null,
            cancellationToken.IsCancellationRequested));

        if (request.Content is not null)
        {
            _requestContents.Add(request.Content);
        }

        if (AttachRequestDisposalProbe)
        {
            DisposalTrackingContent probe = new("{}");
            _requestProbes.Add(probe);
            request.Content = probe;
        }

        OnAttempt?.Invoke(attempt, cancellationToken);

        HttpResponseMessage response = _outcomes[Math.Min(attempt - 1, _outcomes.Length - 1)](attempt);
        switch (response)
        {
            case DisposalTrackingResponse tracked:
                _responses.Add(tracked);
                break;
            case CancellableTrackedResponse cancellable:
                _cancellableResponses.Add(cancellable);
                break;
        }

        return response;
    }
}

/// <summary>
/// Scripted attempt outcomes. Every response carries disposal-tracking content, so any test can assert the
/// per-attempt response lifetime without a second handler.
/// </summary>
internal static class RetryOutcomes
{
    internal const string SuccessBody = """{"outcome":"succeeded"}""";

    internal static RetryAttemptOutcome Success(string body = SuccessBody)
    {
        return _ => Tracked(HttpStatusCode.OK, body);
    }

    /// <summary>
    /// A successful response whose body read observes the caller's token, for the case where cancellation
    /// arrives after the response was handed over but before it was consumed.
    /// </summary>
    internal static RetryAttemptOutcome CancellableSuccess(string body = SuccessBody)
    {
        return _ => new CancellableTrackedResponse(HttpStatusCode.OK, new CancellableTrackedContent(body));
    }

    internal static RetryAttemptOutcome NoContent()
    {
        return _ => Tracked(HttpStatusCode.NoContent, string.Empty);
    }

    /// <summary>
    /// A bare provider redirect, the shape the official decline operation answers with.
    /// </summary>
    internal static RetryAttemptOutcome Redirect(string location)
    {
        return _ =>
        {
            DisposalTrackingResponse response = Tracked(HttpStatusCode.Redirect, string.Empty);
            response.Headers.TryAddWithoutValidation("Location", location);
            return response;
        };
    }

    /// <summary>
    /// A failed HTTP response. <paramref name="body"/> and <paramref name="configure"/> receive the attempt
    /// number, so exhaustion tests can prove that only the last response's evidence survives.
    /// </summary>
    internal static RetryAttemptOutcome Failure(
        HttpStatusCode status,
        Func<int, string>? body = null,
        Action<HttpResponseMessage, int>? configure = null)
    {
        return attempt =>
        {
            DisposalTrackingResponse response = Tracked(status, body?.Invoke(attempt) ?? string.Empty);
            configure?.Invoke(response, attempt);
            return response;
        };
    }

    /// <summary>
    /// A transport failure: the handler throws instead of answering.
    /// </summary>
    internal static RetryAttemptOutcome Throws(Func<int, Exception> failure)
    {
        return attempt => throw failure(attempt);
    }

    private static DisposalTrackingResponse Tracked(HttpStatusCode status, string body)
    {
        return new DisposalTrackingResponse(status, new DisposalTrackingContent(body));
    }
}

/// <summary>
/// Response content that records disposal and observes the cancellation token while its body is read.
/// </summary>
/// <remarks>
/// <see cref="DisposalTrackingContent"/> is pre-buffered, so reading it cannot be interrupted. This variant
/// honours the token in the cancellable <see cref="HttpContent.SerializeToStreamAsync(Stream, TransportContext, CancellationToken)"/>
/// overload, which is what makes "cancelled while the body was being read" a deterministic state to test
/// rather than a race with whichever await the runtime happens to check first.
/// </remarks>
internal sealed class CancellableTrackedContent : HttpContent
{
    private readonly byte[] _payload;

    internal CancellableTrackedContent(string payload)
    {
        _payload = Encoding.UTF8.GetBytes(payload);
        Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
    }

    internal bool Disposed { get; private set; }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return SerializeToStreamAsync(stream, context, CancellationToken.None);
    }

    protected override async Task SerializeToStreamAsync(
        Stream stream,
        TransportContext? context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await stream.WriteAsync(_payload, cancellationToken).ConfigureAwait(false);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _payload.Length;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}

/// <summary>
/// Response whose body read can be cancelled, and which records whether it and its content were disposed.
/// </summary>
internal sealed class CancellableTrackedResponse : HttpResponseMessage
{
    internal CancellableTrackedResponse(HttpStatusCode status, CancellableTrackedContent content)
        : base(status)
    {
        Content = content;
        TrackedContent = content;
    }

    internal CancellableTrackedContent TrackedContent { get; }

    internal bool Disposed { get; private set; }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}

/// <summary>
/// One captured log entry, with everything a sink could write.
/// </summary>
internal sealed record RetryLogEntry(LogLevel Level, string Message, IReadOnlyList<string> StateValues)
{
    internal IEnumerable<string> AllText => new[] { Message }.Concat(StateValues);
}

/// <summary>
/// Logger that captures rendered messages and structured state, and can run a callback the moment an entry
/// is written.
/// </summary>
/// <remarks>
/// The callback is what makes "cancelled during the retry delay" deterministic: the retry warning is written
/// immediately before the delay is awaited, so cancelling from the callback lands inside the wait without a
/// sleep or a timing threshold.
/// </remarks>
internal sealed class RetryLogRecorder : ILogger
{
    private readonly List<RetryLogEntry> _entries = [];

    internal Action<RetryLogEntry>? OnEntry { get; set; }

    internal IReadOnlyList<RetryLogEntry> Entries => _entries;

    /// <summary>
    /// Every piece of text the whole capture could put in front of an operator.
    /// </summary>
    internal IEnumerable<string> AllText => _entries.SelectMany(static entry => entry.AllText);

    internal IEnumerable<RetryLogEntry> RetryWarnings => _entries
        .Where(static entry => entry.Level == LogLevel.Warning
            && entry.Message.StartsWith("Retry ", StringComparison.Ordinal));

    /// <summary>
    /// The delay each retry warning reported, in milliseconds, in order.
    /// </summary>
    internal IReadOnlyList<double> RetryDelaysMilliseconds => RetryWarnings
        .Select(static entry => ReadDelay(entry))
        .ToArray();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        List<string> stateValues = [];
        if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            foreach (KeyValuePair<string, object?> pair in pairs)
            {
                stateValues.Add(
                    $"{pair.Key}={Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty}");
            }
        }
        else
        {
            stateValues.Add(Convert.ToString(state, CultureInfo.InvariantCulture) ?? string.Empty);
        }

        // A sink writes the exception too, so a leak assertion has to see whatever the SDK passed here.
        if (exception is not null)
        {
            stateValues.Add(exception.ToString());
        }

        RetryLogEntry entry = new(logLevel, formatter(state, exception), stateValues);
        _entries.Add(entry);
        OnEntry?.Invoke(entry);
    }

    private static double ReadDelay(RetryLogEntry entry)
    {
        const string Key = "DelayMilliseconds=";

        string? value = entry.StateValues
            .FirstOrDefault(state => state.StartsWith(Key, StringComparison.Ordinal))?[Key.Length..];

        return value is not null && double.TryParse(value, CultureInfo.InvariantCulture, out double delay)
            ? delay
            : throw new InvalidOperationException(
                $"The retry warning did not report a parseable delay: '{entry.Message}'.");
    }

    private sealed class NullScope : IDisposable
    {
        internal static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}

/// <summary>
/// Request body for the retry matrix. Carries a single marker so that "the repeat sent the same body" is a
/// real assertion against a value that appears nowhere else.
/// </summary>
internal sealed class RetryProbePayload
{
    public string? Marker { get; set; }
}

/// <summary>
/// Response body for the retry matrix. Parameterless-constructible, so it also fits the helper that accepts
/// a <c>204</c>.
/// </summary>
internal sealed class RetryProbeResult
{
    public string? Outcome { get; set; }
}

/// <summary>
/// Test-only service that exposes the protected transport helpers of <see cref="BaseService"/> unchanged.
/// </summary>
/// <remarks>
/// The retry loop lives in <see cref="BaseService"/> and is shared by every operation, so the matrix drives
/// the real production loop through the real helpers instead of re-implementing it or binding the assertions
/// to one business DTO.
/// </remarks>
internal sealed class RetryProbeService : BaseService
{
    internal RetryProbeService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    internal Task<TResponse> GetJsonAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        return GetAsync<TResponse>(endpoint, cancellationToken);
    }

    internal Task<TResponse> PostJsonAsync<TResponse>(
        string endpoint,
        RetryProbePayload request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<RetryProbePayload, TResponse>(endpoint, request, cancellationToken);
    }

    internal Task<RetryProbeResult> PostJsonAllowingNoContentAsync(
        string endpoint,
        RetryProbePayload request,
        CancellationToken cancellationToken = default)
    {
        return PostAsyncWithNoContent<RetryProbePayload, RetryProbeResult>(endpoint, request, cancellationToken);
    }

    internal Task<TResponse> PatchJsonAsync<TResponse>(
        string endpoint,
        RetryProbePayload request,
        CancellationToken cancellationToken = default)
    {
        return PatchAsync<RetryProbePayload, TResponse>(endpoint, request, cancellationToken);
    }

    internal Task<TResponse> PostWithoutBodyJsonAsync<TResponse>(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        return PostWithoutBodyAsync<TResponse>(endpoint, endpoint, cancellationToken);
    }

    internal Task<TResponse> DeleteJsonAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        return DeleteAsync<TResponse>(endpoint, cancellationToken);
    }

    internal Task<TResponse> DeleteWithBodyAsync<TResponse>(
        string endpoint,
        RetryProbePayload request,
        CancellationToken cancellationToken = default)
    {
        return DeleteAsync<RetryProbePayload, TResponse>(endpoint, endpoint, request, cancellationToken);
    }

    /// <summary>
    /// The retry loop itself, for failures that no transport helper can produce — a manually constructed SDK
    /// exception, or a transport exception raised before a response exists.
    /// </summary>
    internal Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        return ExecuteWithRetryAsync(operation, cancellationToken);
    }
}

/// <summary>
/// Shared setup for the EXP-356 retry contract matrix.
/// </summary>
internal static class RetryContractContext
{
    /// <summary>
    /// Reserved TLD: even a regression that bypassed the scripted handler could not resolve this host.
    /// </summary>
    internal const string BaseUrl = "https://retry.invalid";

    internal const string Endpoint = "/api/retry-probe/v1/resource";

    internal const string DeclineEndpoint = "/api/payment-instructions/v1/decline";

    internal const string JsonContentType = "application/json; charset=utf-8";

    /// <summary>
    /// Unmistakably synthetic leak markers: long, unique, and obviously not credentials, so a log-leak
    /// assertion cannot pass by accident and a secret scanner has nothing to flag.
    /// </summary>
    internal const string RequestBodyMarker = "request-body-marker-must-never-be-logged-EXP356";

    internal const string ProviderMessageMarker = "provider-message-marker-must-never-be-logged-EXP356";

    internal const string RawBodyMarker = "raw-body-marker-must-never-be-logged-EXP356";

    internal const string TransportMessageMarker = "transport-message-marker-must-never-be-logged-EXP356";

    internal const string PasswordPlaceholder = "unit-test-placeholder";

    /// <summary>
    /// A provider error body carrying a marker in every field a log could pick up.
    /// </summary>
    internal static string ErrorBody(int attempt)
    {
        return $"{{\"code\":\"attempt_{attempt}_code\",\"error_id\":\"attempt-{attempt}-request-id\"," +
            $"\"message\":\"{ProviderMessageMarker}\",\"detail\":\"{RawBodyMarker}\"}}";
    }

    internal static RetryProbePayload Payload()
    {
        return new RetryProbePayload { Marker = RequestBodyMarker };
    }

    /// <summary>
    /// The body the SDK serializes for <see cref="Payload"/>, written out literally so the assertion does not
    /// mirror the production serializer.
    /// </summary>
    internal const string ExpectedPayloadJson = $"{{\"marker\":\"{RequestBodyMarker}\"}}";

    internal static RozetkaPayConfiguration Configuration(RetryPolicy? retryPolicy = null)
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = BaseUrl,
            Login = "unit-test-login",
            Password = PasswordPlaceholder,
            RetryPolicy = retryPolicy ?? RetryPolicy.None,
            UserAgent = "SYT.RozetkaPay.Tests"
        };
    }

    /// <summary>
    /// Retry enabled with no waiting at all, for every test whose subject is the decision rather than the
    /// delay.
    /// </summary>
    internal static RetryPolicy Immediate(int maxRetryAttempts = 1, IEnumerable<HttpStatusCode>? statusCodes = null)
    {
        RetryPolicy policy = new()
        {
            Enabled = true,
            MaxRetryAttempts = maxRetryAttempts,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            BackoffStrategy = BackoffStrategy.Fixed
        };

        if (statusCodes is not null)
        {
            policy.RetriableStatusCodes = [.. statusCodes];
        }

        return policy;
    }

    internal static RetryProbeService Service(
        ScriptedRetryHandler handler,
        RetryPolicy? retryPolicy = null,
        ILogger? logger = null)
    {
        return new RetryProbeService(Configuration(retryPolicy), Client(handler), logger);
    }

    internal static HttpClient Client(HttpMessageHandler handler)
    {
        return new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
    }
}
