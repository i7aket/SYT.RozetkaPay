using System.Collections;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// One request as the HTTP handler actually saw it. The request target is captured as the handler-visible
/// <see cref="Uri"/>, not as the string the service built, so a double-escape or a value escaped at the
/// wrong insertion point is visible.
/// </summary>
internal sealed record Exp354Request(
    HttpMethod Method,
    Uri RequestUri,
    string? Body,
    string? ContentType,
    bool HasContent,
    IReadOnlyDictionary<string, string[]> Headers,
    bool CancellationRequestedOnArrival);

/// <summary>
/// Recording handler for the EXP-354 services. Answers with a canned response and never reaches a
/// network, so no test can contact RozetkaPay.
/// </summary>
internal sealed class RecordingHandler : HttpMessageHandler
{
    private readonly List<Exp354Request> _requests = [];
    private readonly Func<int, HttpResponseMessage> _responseFactory;

    private RecordingHandler(Func<int, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    /// <summary>
    /// Runs inside the handler before the response is produced. Used to cancel a token while the
    /// transport is in flight, which is only observable if the caller token really was propagated.
    /// </summary>
    internal Action<HttpRequestMessage, CancellationToken>? OnRequest { get; set; }

    internal IReadOnlyList<Exp354Request> Requests => _requests;

    internal static RecordingHandler Json(string body)
    {
        return new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }

    internal static RecordingHandler Error(
        HttpStatusCode status,
        string body = """{"code":"declined","message":"Provider rejected the request"}""")
    {
        return new RecordingHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }

    /// <summary>
    /// Answers a redirect without a body, the way the official decline operation does.
    /// </summary>
    internal static RecordingHandler Redirect(string? location)
    {
        return new RecordingHandler(_ =>
        {
            HttpResponseMessage response = new(HttpStatusCode.Redirect);
            if (location is not null)
            {
                // TryAddWithoutValidation so that an unparseable value survives to the SDK, which is what
                // a misbehaving provider would send.
                response.Headers.TryAddWithoutValidation("Location", location);
            }

            return response;
        });
    }

    /// <summary>
    /// Answers with a status the decline operation does not expect, without a redirect.
    /// </summary>
    internal static RecordingHandler Status(HttpStatusCode status)
    {
        return new RecordingHandler(_ => new HttpResponseMessage(status));
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

        OnRequest?.Invoke(request, cancellationToken);

        _requests.Add(new Exp354Request(
            request.Method,
            request.RequestUri!,
            body,
            request.Content?.Headers.ContentType?.ToString(),
            request.Content is not null,
            headers,
            cancellationToken.IsCancellationRequested));

        cancellationToken.ThrowIfCancellationRequested();

        return _responseFactory(_requests.Count);
    }
}

/// <summary>
/// Logger that keeps both the rendered message and the structured state of every entry.
/// </summary>
/// <remarks>
/// <see cref="TestLogger{T}"/> records rendered messages only. A caller identifier can leak through the
/// structured state alone — a log sink writes the state, not just the formatted string — so leak
/// assertions need both.
/// </remarks>
internal sealed class RecordingLogger : ILogger
{
    internal List<string> Messages { get; } = [];

    /// <summary>
    /// Every structured key/value pair of every entry, rendered invariantly.
    /// </summary>
    internal List<string> StateValues { get; } = [];

    /// <summary>
    /// Rendered messages and structured values together, for a single "does this text appear anywhere"
    /// assertion.
    /// </summary>
    internal IEnumerable<string> AllText => Messages.Concat(StateValues);

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
        Messages.Add(formatter(state, exception));

        if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            foreach (KeyValuePair<string, object?> pair in pairs)
            {
                StateValues.Add(
                    $"{pair.Key}={Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty}");
            }
        }
        else if (state is IEnumerable and not string)
        {
            foreach (object? item in (IEnumerable)state)
            {
                StateValues.Add(Convert.ToString(item, CultureInfo.InvariantCulture) ?? string.Empty);
            }
        }
        else
        {
            StateValues.Add(Convert.ToString(state, CultureInfo.InvariantCulture) ?? string.Empty);
        }
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
/// Minimal loopback HTTP server used by the non-provider redirect test.
/// </summary>
/// <remarks>
/// Binds to <c>127.0.0.1</c> on an ephemeral port, so nothing leaves the machine and no real host is
/// ever contacted. Every wait is bounded, and the listener is stopped on dispose, so a regression cannot
/// hang the suite.
/// </remarks>
internal sealed class LoopbackServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly List<LoopbackRequest> _requests = [];
    private readonly Lock _gate = new();
    private readonly Task _loop;
    private readonly Func<LoopbackRequest, HttpListenerResponse, Task> _responder;

    private LoopbackServer(Func<LoopbackRequest, HttpListenerResponse, Task> responder)
    {
        _responder = responder;

        // HttpListener cannot report an ephemeral port it chose, so a free port is found by probing a
        // small range. Each attempt uses a fresh listener: a failed Start() closes the instance it was
        // called on, so the same one cannot be retried.
        (_listener, Port) = StartOnFreePort();
        _loop = Task.Run(AcceptLoopAsync);
    }

    internal int Port { get; }

    internal string BaseUrl => $"http://127.0.0.1:{Port}";

    internal IReadOnlyList<LoopbackRequest> Requests
    {
        get
        {
            lock (_gate)
            {
                return _requests.ToArray();
            }
        }
    }

    internal static LoopbackServer Start(Func<LoopbackRequest, HttpListenerResponse, Task> responder)
    {
        return new LoopbackServer(responder);
    }

    /// <summary>
    /// Answers every request with a <c>302</c> to <paramref name="location"/> and no body.
    /// </summary>
    internal static LoopbackServer Redirecting(string location)
    {
        return Start((_, response) =>
        {
            response.StatusCode = (int)HttpStatusCode.Redirect;
            response.Headers["Location"] = location;
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Answers every request with <c>200</c> and a body, so that a followed redirect is unmistakable.
    /// </summary>
    internal static LoopbackServer Answering(string body)
    {
        return Start(async (_, response) =>
        {
            byte[] payload = Encoding.UTF8.GetBytes(body);
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "text/plain";
            response.ContentLength64 = payload.Length;
            await response.OutputStream.WriteAsync(payload).ConfigureAwait(false);
        });
    }

    public void Dispose()
    {
        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch (ObjectDisposedException)
        {
            // Already closed.
        }

        // Bounded: the accept loop ends as soon as the listener is closed.
        _loop.Wait(TimeSpan.FromSeconds(5));
    }

    private static (HttpListener Listener, int Port) StartOnFreePort()
    {
        for (int port = 34000; port < 34200; port++)
        {
            HttpListener candidate = new();
            candidate.Prefixes.Add($"http://127.0.0.1:{port}/");

            try
            {
                candidate.Start();
                return (candidate, port);
            }
            catch (HttpListenerException)
            {
                // Port taken by another process or another parallel test run. Start() already closed this
                // listener, so the next attempt needs a new one.
                ((IDisposable)candidate).Dispose();
            }
        }

        throw new InvalidOperationException("No free loopback port available for the redirect test.");
    }

    private async Task AcceptLoopAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            LoopbackRequest recorded = new(
                context.Request.HttpMethod,
                context.Request.RawUrl ?? string.Empty,
                context.Request.Headers.AllKeys
                    .Where(key => key is not null)
                    .ToDictionary(
                        key => key!,
                        key => context.Request.Headers[key] ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase));

            lock (_gate)
            {
                _requests.Add(recorded);
            }

            try
            {
                await _responder(recorded, context.Response).ConfigureAwait(false);
                context.Response.Close();
            }
            catch (HttpListenerException)
            {
                // Client went away; nothing to report.
            }
            catch (ObjectDisposedException)
            {
                // Listener closed mid-response.
            }
        }
    }
}

/// <summary>
/// One request as a <see cref="LoopbackServer"/> received it off the wire.
/// </summary>
internal sealed record LoopbackRequest(
    string Method,
    string RawUrl,
    IReadOnlyDictionary<string, string> Headers);

/// <summary>
/// Response content that records whether it was disposed.
/// </summary>
/// <remarks>
/// A buffered <see cref="HttpResponseMessage"/> holds a network stream and a connection until it is
/// disposed, so "did the SDK dispose it" is a real behavioural property. Nothing else in the test disposes
/// this content, so the flag is only ever set through the response the SDK owns.
/// </remarks>
internal sealed class DisposalTrackingContent : HttpContent
{
    private readonly byte[] _payload;

    internal DisposalTrackingContent(string payload)
    {
        _payload = Encoding.UTF8.GetBytes(payload);
        Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
    }

    internal bool Disposed { get; private set; }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return stream.WriteAsync(_payload, 0, _payload.Length);
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
/// Response that records whether it was disposed, together with its content.
/// </summary>
internal sealed class DisposalTrackingResponse : HttpResponseMessage
{
    internal DisposalTrackingResponse(HttpStatusCode status, DisposalTrackingContent content)
        : base(status)
    {
        Content = content;
        TrackedContent = content;
    }

    internal DisposalTrackingContent TrackedContent { get; }

    internal bool Disposed { get; private set; }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}

/// <summary>
/// Handler that answers with <see cref="DisposalTrackingResponse"/> instances and keeps them for
/// inspection after the call has returned.
/// </summary>
internal sealed class DisposalTrackingHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _body;
    private readonly string? _location;
    private readonly List<DisposalTrackingResponse> _responses = [];

    private DisposalTrackingHandler(HttpStatusCode status, string body, string? location)
    {
        _status = status;
        _body = body;
        _location = location;
    }

    internal IReadOnlyList<DisposalTrackingResponse> Responses => _responses;

    internal static DisposalTrackingHandler Json(HttpStatusCode status, string body)
    {
        return new DisposalTrackingHandler(status, body, location: null);
    }

    internal static DisposalTrackingHandler Redirect(string location)
    {
        return new DisposalTrackingHandler(HttpStatusCode.Redirect, string.Empty, location);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        DisposalTrackingResponse response = new(_status, new DisposalTrackingContent(_body));
        if (_location is not null)
        {
            response.Headers.TryAddWithoutValidation("Location", _location);
        }

        _responses.Add(response);
        return Task.FromResult<HttpResponseMessage>(response);
    }
}

/// <summary>
/// One captured log entry, including everything a sink could write: category, rendered message, structured
/// state, and enclosing scopes.
/// </summary>
internal sealed record CapturedLogEntry(
    string Category,
    LogLevel Level,
    string Message,
    IReadOnlyList<string> StateValues,
    IReadOnlyList<string> ScopeValues)
{
    /// <summary>
    /// Every piece of text this entry could put in front of an operator.
    /// </summary>
    internal IEnumerable<string> AllText =>
        new[] { Category, Message }.Concat(StateValues).Concat(ScopeValues);
}

/// <summary>
/// Logging provider that captures every category, message, structured value, and scope.
/// </summary>
/// <remarks>
/// Used for the DI-level logging contract: a leak assertion has to cover the whole logging pipeline, not
/// just the SDK's own service logs. In particular <c>AddHttpClient</c> installs its own
/// <c>System.Net.Http.HttpClient.*</c> handler logging, which the SDK's static log labels cannot influence.
/// </remarks>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<CapturedLogEntry> _entries = [];
    private readonly Lock _gate = new();

    internal IReadOnlyList<CapturedLogEntry> Entries
    {
        get
        {
            lock (_gate)
            {
                return _entries.ToArray();
            }
        }
    }

    internal IEnumerable<string> AllText => Entries.SelectMany(static entry => entry.AllText);

    /// <summary>
    /// Categories the built-in <see cref="IHttpClientFactory"/> logging writes under.
    /// </summary>
    internal IEnumerable<string> FactoryHttpCategories => Entries
        .Select(static entry => entry.Category)
        .Where(static category => category.StartsWith("System.Net.Http.HttpClient.", StringComparison.Ordinal))
        .Distinct(StringComparer.Ordinal);

    public ILogger CreateLogger(string categoryName)
    {
        return new CapturingLogger(categoryName, Add);
    }

    public void Dispose()
    {
    }

    private void Add(CapturedLogEntry entry)
    {
        lock (_gate)
        {
            _entries.Add(entry);
        }
    }

    private sealed class CapturingLogger(string category, Action<CapturedLogEntry> sink) : ILogger
    {
        private readonly List<string> _scopes = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            string rendered = Render(state);
            _scopes.Add(rendered);
            return new ScopeHandle(_scopes, rendered);
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
                    stateValues.Add($"{pair.Key}={Render(pair.Value)}");
                }
            }
            else
            {
                stateValues.Add(Render(state));
            }

            if (exception is not null)
            {
                stateValues.Add(exception.ToString());
            }

            sink(new CapturedLogEntry(
                category,
                logLevel,
                formatter(state, exception),
                stateValues,
                [.. _scopes]));
        }

        private static string Render(object? value)
        {
            return value switch
            {
                null => string.Empty,
                string text => text,
                IEnumerable<KeyValuePair<string, object?>> pairs =>
                    string.Join(", ", pairs.Select(pair => $"{pair.Key}={Render(pair.Value)}")),
                IEnumerable<string> items => string.Join(", ", items),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };
        }

        private sealed class ScopeHandle(List<string> scopes, string rendered) : IDisposable
        {
            public void Dispose()
            {
                scopes.Remove(rendered);
            }
        }
    }
}

/// <summary>
/// Shared setup for the EXP-354 service tests.
/// </summary>
internal static class Exp354TestContext
{
    /// <summary>
    /// Fake host. Every request is intercepted by a recording handler, so no DNS lookup or network
    /// traffic can occur even if a test regresses.
    /// </summary>
    internal const string BaseUrl = "https://unit.test";

    /// <summary>
    /// Raw caller input covering every character that could break out of its insertion point: space,
    /// '+', '/', '&amp;', '=', '?', '#', '%', and non-ASCII.
    /// </summary>
    internal const string HostileRawId = "id +/&=?#% Привіт";

    /// <summary>
    /// <see cref="HostileRawId"/> percent-encoded exactly once, written as a literal so that the
    /// assertion cannot mirror the production helper.
    /// </summary>
    internal const string HostileEncodedId =
        "id%20%2B%2F%26%3D%3F%23%25%20%D0%9F%D1%80%D0%B8%D0%B2%D1%96%D1%82";

    /// <summary>
    /// Caller input is raw, never pre-encoded: a literal '%' becomes "%25" exactly once.
    /// </summary>
    internal const string LooksEncodedRawId = "already%2Fencoded";

    internal const string LooksEncodedExpectedId = "already%252Fencoded";

    /// <summary>
    /// Distinctive, unmistakably synthetic leak markers. They are long and unique so a log-leak
    /// assertion cannot pass by accident, and obviously not credentials so secret scanners have nothing
    /// to flag.
    /// </summary>
    internal const string SecretSubscriptionId = "subscription-id-placeholder-must-never-be-logged-EXP354";

    internal const string SecretExternalId = "external-id-placeholder-must-never-be-logged-EXP354";

    internal const string SecretMerchantId = "merchant-id-placeholder-must-never-be-logged-EXP354";

    internal const string SecretProjectId = "project-id-placeholder-must-never-be-logged-EXP354";

    internal const string SecretInstructionId = "instruction-id-placeholder-must-never-be-logged-EXP354";

    internal const string CustomerAuthPlaceholder = "customer-auth-placeholder-not-a-real-token-EXP354";

    internal const string JsonContentType = "application/json; charset=utf-8";

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

    internal static RozetkaPayConfiguration WithCustomerAuth()
    {
        RozetkaPayConfiguration configuration = CreateConfiguration();
        configuration.CustomerAuth = CustomerAuthPlaceholder;
        return configuration;
    }

    internal static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
    }

    internal static SubscriptionService Subscriptions(
        RecordingHandler handler,
        RozetkaPayConfiguration? configuration = null,
        ILogger? logger = null)
    {
        return new SubscriptionService(configuration ?? CreateConfiguration(), CreateHttpClient(handler), logger);
    }

    internal static InStorePaymentService InStorePayments(
        RecordingHandler handler,
        RozetkaPayConfiguration? configuration = null,
        ILogger? logger = null)
    {
        return new InStorePaymentService(configuration ?? CreateConfiguration(), CreateHttpClient(handler), logger);
    }

    internal static PartnerService Partners(
        RecordingHandler handler,
        RozetkaPayConfiguration? configuration = null,
        ILogger? logger = null)
    {
        return new PartnerService(configuration ?? CreateConfiguration(), CreateHttpClient(handler), logger);
    }

    /// <summary>
    /// Payment-instruction service whose decline transport is the supplied handler. The decline client
    /// is created here, so it is caller-owned: the service must not dispose it.
    /// </summary>
    internal static PaymentInstructionService PaymentInstructions(
        RecordingHandler authenticatedHandler,
        HttpClient declineHttpClient,
        RozetkaPayConfiguration? configuration = null,
        ILogger? logger = null)
    {
        return new PaymentInstructionService(
            configuration ?? CreateConfiguration(),
            CreateHttpClient(authenticatedHandler),
            declineHttpClient,
            logger);
    }

    /// <summary>
    /// A decline client over <paramref name="handler"/>, carrying no credential header. Mirrors what the
    /// DI-registered non-redirect named client looks like from the service's point of view.
    /// </summary>
    internal static HttpClient CreateDeclineHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
    }

    /// <summary>
    /// The serializer options the services actually use, reached through a throwaway subclass so that
    /// expected JSON can be compared without duplicating the option set.
    /// </summary>
    internal static JsonSerializerOptions SerializerOptions()
    {
        return SerializerOptionsProbe.Instance.Options;
    }

    private sealed class SerializerOptionsProbe : BaseService
    {
        internal static readonly SerializerOptionsProbe Instance = new();

        private SerializerOptionsProbe()
            : base(CreateConfiguration(), new HttpClient())
        {
        }

        internal JsonSerializerOptions Options => GetJsonSerializerOptions();
    }
}
